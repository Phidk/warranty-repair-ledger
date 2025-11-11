using WarrantyRepairLedger.Models;
using WarrantyRepairLedger.Options;
using WarrantyRepairLedger.Services;

namespace WarrantyRepairLedger.Tests;

public class WarrantyEvaluatorTests
{
    private readonly WarrantyEvaluator _evaluator = new(Microsoft.Extensions.Options.Options.Create(new WarrantyOptions
    {
        DefaultMonths = 24
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
}
