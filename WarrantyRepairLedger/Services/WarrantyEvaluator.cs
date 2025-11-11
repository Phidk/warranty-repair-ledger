using WarrantyRepairLedger.Models;
using WarrantyRepairLedger.Options;

namespace WarrantyRepairLedger.Services;

public class WarrantyEvaluator
{
    private readonly WarrantyOptions _options;

    public WarrantyEvaluator(Microsoft.Extensions.Options.IOptions<WarrantyOptions> options)
    {
        _options = options.Value;
    }

    // Calculates whether a product is still covered using purchase date plus warranty months
    public WarrantyWindow Evaluate(Product product, DateOnly? referenceDate = null)
    {
        var expiresOn = GetExpirationDate(product);
        var today = referenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var inWarranty = today <= expiresOn;
        var reason = inWarranty
            ? $"Warranty valid until {expiresOn:yyyy-MM-dd}"
            : $"Warranty expired on {expiresOn:yyyy-MM-dd}";

        return new WarrantyWindow(inWarranty, expiresOn, reason);
    }

    // Quick helper to see if a warranty ends within an upcoming window
    public bool IsExpiringWithin(Product product, int days, DateOnly? referenceDate = null)
    {
        var expiresOn = GetExpirationDate(product);
        var today = referenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var remainingDays = expiresOn.DayNumber - today.DayNumber;
        return remainingDays >= 0 && remainingDays <= days;
    }

    // Normalizes warranty months and returns the final expiration date
    public DateOnly GetExpirationDate(Product product)
    {
        var months = product.WarrantyMonths > 0 ? product.WarrantyMonths : _options.DefaultMonths;
        return product.PurchaseDate.AddMonths(months);
    }
}

public readonly record struct WarrantyWindow(bool InWarranty, DateOnly ExpiresOn, string Reason);
