using System.Collections.Generic;
using WarrantyRepairLedger.Models;
using WarrantyRepairLedger.Options;
using WarrantyRepairLedger.Services;

namespace WarrantyRepairLedger.Tests;

    public class WarrantyEvaluatorTests
    {
        private readonly WarrantyEvaluator _evaluator = new(Microsoft.Extensions.Options.Options.Create(new WarrantyOptions
        {
            DefaultMonths = 24,
            RepairExtensionMonths = 12
        }));

    [Fact]
    public void Evaluate_ReturnsInWarranty_WhenWithinWindow()
    {
        var product = new Product
        {
            Name = "Laptop",
            Serial = "ABC123",
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
            WarrantyMonths = 12
        };

        var window = _evaluator.Evaluate(product, DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.True(window.InWarranty);
        Assert.Equal(product.PurchaseDate.AddMonths(product.WarrantyMonths), window.ExpiresOn);
    }

    [Fact]
    public void Evaluate_UsesDefaultWhenWarrantyMonthsInvalid()
    {
        var product = new Product
        {
            Name = "Camera",
            Serial = "XYZ",
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-25)),
            WarrantyMonths = 0
        };

        var window = _evaluator.Evaluate(product, DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.False(window.InWarranty);
        Assert.Equal(product.PurchaseDate.AddMonths(24), window.ExpiresOn);
    }

    [Fact]
    public void IsExpiringWithin_DetectsUpcomingExpiry()
    {
        var product = new Product
        {
            Name = "Tablet",
            Serial = "TAB001",
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-23)),
            WarrantyMonths = 24
        };

        var result = _evaluator.IsExpiringWithin(product, 40, DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.True(result);
    }

    [Fact]
    public void GetExpirationDate_ExtendsWhenRepairCompletesWithinCoverage()
    {
        var purchaseDate = new DateOnly(2026, 8, 1);
        var baseExpiry = purchaseDate.AddMonths(24);
        var openedAt = new DateTimeOffset(purchaseDate.ToDateTime(TimeOnly.MinValue).AddMonths(6), TimeSpan.Zero);
        var closedAt = openedAt.AddMonths(1);
        var product = new Product
        {
            Name = "Console",
            Serial = "XYZ789",
            PurchaseDate = purchaseDate,
            WarrantyMonths = 24,
            Repairs = new List<Repair>
            {
                new()
                {
                    Status = RepairStatus.Fixed,
                    OpenedAt = openedAt,
                    ClosedAt = closedAt
                }
            }
        };

        var expiresOn = _evaluator.GetExpirationDate(product);

        var expectedExtension = baseExpiry.AddMonths(12);
        Assert.Equal(expectedExtension, expiresOn);
    }

    [Fact]
    public void GetExpirationDate_IgnoresRepairsCompletedAfterCoverage()
    {
        var product = new Product
        {
            Name = "Speaker",
            Serial = "SPEAK1",
            PurchaseDate = new DateOnly(2026, 8, 1),
            WarrantyMonths = 24,
            Repairs = new List<Repair>
            {
                new()
                {
                    Status = RepairStatus.Fixed,
                    // Opened well after the base legal guarantee has already ended
                    OpenedAt = new DateTimeOffset(new DateTime(2029, 9, 1), TimeSpan.Zero),
                    ClosedAt = new DateTimeOffset(new DateTime(2029, 9, 15), TimeSpan.Zero)
                }
            }
        };

        var expiresOn = _evaluator.GetExpirationDate(product);

        Assert.Equal(product.PurchaseDate.AddMonths(24), expiresOn);
    }
}
