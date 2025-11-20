using System.Linq;
using WarrantyRepairLedger.Models;
using WarrantyRepairLedger.Options;

namespace WarrantyRepairLedger.Services;

public class WarrantyEvaluator
{
    private readonly WarrantyOptions _options;
    private static readonly DateOnly ExtensionEffectiveDate = new(2026, 7, 31);

    public WarrantyEvaluator(Microsoft.Extensions.Options.IOptions<WarrantyOptions> options)
    {
        _options = options.Value;
    }

    // Calculates whether a product is still covered, taking the one-time legal guarantee extension into account
    public WarrantyWindow Evaluate(Product product, DateOnly? referenceDate = null, IEnumerable<Repair>? repairs = null)
    {
        var today = referenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var (baseExpiry, finalExpiry, extended) = ComputeExpiry(product, repairs);
        var inWarranty = today <= finalExpiry;
        var reason = inWarranty
            ? extended
                ? $"Within extended legal guarantee until {finalExpiry:yyyy-MM-dd}"
                : $"Within legal guarantee until {finalExpiry:yyyy-MM-dd}"
            : $"Legal guarantee expired on {finalExpiry:yyyy-MM-dd}";

        return new WarrantyWindow(inWarranty, finalExpiry, reason);
    }

    // Quick helper to see if a warranty ends within an upcoming window
    public bool IsExpiringWithin(Product product, int days, DateOnly? referenceDate = null, IEnumerable<Repair>? repairs = null)
    {
        var expiresOn = GetExpirationDate(product, repairs);
        var today = referenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var remainingDays = expiresOn.DayNumber - today.DayNumber;
        return remainingDays >= 0 && remainingDays <= days;
    }

    // Normalizes warranty months, then applies a one-time extension when a qualifying repair happens within the legal guarantee window
    public DateOnly GetExpirationDate(Product product, IEnumerable<Repair>? repairs = null)
    {
        var (_, finalExpiry, _) = ComputeExpiry(product, repairs);
        return finalExpiry;
    }

    private (DateOnly BaseExpiry, DateOnly FinalExpiry, bool Extended) ComputeExpiry(Product product, IEnumerable<Repair>? repairs)
    {
        var months = product.WarrantyMonths > 0 ? product.WarrantyMonths : _options.DefaultMonths;
        var baseExpiry = product.PurchaseDate.AddMonths(months);

        // EU extension applies only to contracts on/after the effective date
        if (product.PurchaseDate < ExtensionEffectiveDate)
        {
            return (baseExpiry, baseExpiry, false);
        }

        var relevantRepairs = (repairs ?? product.Repairs ?? Enumerable.Empty<Repair>())
            .Where(r => r.Status == RepairStatus.Fixed && r.ClosedAt is not null)
            .Where(r => DateOnly.FromDateTime(r.OpenedAt.UtcDateTime) <= baseExpiry);

        var extended = relevantRepairs.Any();
        var finalExpiry = extended
            ? baseExpiry.AddMonths(_options.RepairExtensionMonths)
            : baseExpiry;

        return (baseExpiry, finalExpiry, extended);
    }
}

public readonly record struct WarrantyWindow(bool InWarranty, DateOnly ExpiresOn, string Reason);
