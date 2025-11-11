using System.Linq;
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

    // Calculates whether a product is still covered, taking the right-to-repair extension into account
    public WarrantyWindow Evaluate(Product product, DateOnly? referenceDate = null, IEnumerable<Repair>? repairs = null)
    {
        var expiresOn = GetExpirationDate(product, repairs);
        var today = referenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var inWarranty = today <= expiresOn;
        var reason = inWarranty
            ? $"Warranty valid until {expiresOn:yyyy-MM-dd}"
            : $"Warranty expired on {expiresOn:yyyy-MM-dd}";

        return new WarrantyWindow(inWarranty, expiresOn, reason);
    }

    // Quick helper to see if a warranty ends within an upcoming window
    public bool IsExpiringWithin(Product product, int days, DateOnly? referenceDate = null, IEnumerable<Repair>? repairs = null)
    {
        var expiresOn = GetExpirationDate(product, repairs);
        var today = referenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var remainingDays = expiresOn.DayNumber - today.DayNumber;
        return remainingDays >= 0 && remainingDays <= days;
    }

    // Normalizes warranty months, then applies any right-to-repair extension derived from completed repairs
    public DateOnly GetExpirationDate(Product product, IEnumerable<Repair>? repairs = null)
    {
        var months = product.WarrantyMonths > 0 ? product.WarrantyMonths : _options.DefaultMonths;
        var expiresOn = product.PurchaseDate.AddMonths(months);

        var relevantRepairs = repairs
            ?? product.Repairs
            ?? Enumerable.Empty<Repair>();

        foreach (var repair in relevantRepairs)
        {
            if (!repair.ConsumerOptedForRepair || repair.Status != RepairStatus.Fixed || repair.ClosedAt is null)
            {
                continue;
            }

            var closedDate = DateOnly.FromDateTime(repair.ClosedAt.Value.UtcDateTime);
            var extensionExpiresOn = closedDate.AddMonths(_options.RepairExtensionMonths);
            if (extensionExpiresOn > expiresOn)
            {
                expiresOn = extensionExpiresOn;
            }
        }

        return expiresOn;
    }
}

public readonly record struct WarrantyWindow(bool InWarranty, DateOnly ExpiresOn, string Reason);
