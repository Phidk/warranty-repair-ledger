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
    public void GetExpirationDate_ExtendsWhenConsumerChoseRepair()
    {
        var closedAt = DateTimeOffset.UtcNow.AddDays(-5);
        var product = new Product
        {
            Name = "Console",
            Serial = "XYZ789",
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-30)),
            WarrantyMonths = 24,
            Repairs = new List<Repair>
            {
                new()
                {
                    ConsumerOptedForRepair = true,
                    Status = RepairStatus.Fixed,
                    OpenedAt = closedAt.AddDays(-10),
                    ClosedAt = closedAt
                }
            }
        };

        var expiresOn = _evaluator.GetExpirationDate(product);

        var expected = DateOnly.FromDateTime(closedAt.UtcDateTime).AddMonths(12);
        Assert.Equal(expected, expiresOn);
    }

    [Fact]
    public void GetExpirationDate_IgnoresRepairsWithoutExtension()
    {
        var product = new Product
        {
            Name = "Speaker",
            Serial = "SPEAK1",
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-10)),
            WarrantyMonths = 12,
            Repairs = new List<Repair>
            {
                new()
                {
                    ConsumerOptedForRepair = false,
                    Status = RepairStatus.Fixed,
                    OpenedAt = DateTimeOffset.UtcNow.AddDays(-20),
                    ClosedAt = DateTimeOffset.UtcNow.AddDays(-10)
                }
            }
        };

        var expiresOn = _evaluator.GetExpirationDate(product);

        Assert.Equal(product.PurchaseDate.AddMonths(12), expiresOn);
    }
}
