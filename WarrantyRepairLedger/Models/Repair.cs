namespace WarrantyRepairLedger.Models;

public class Repair
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public DateTimeOffset OpenedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public RepairStatus Status { get; set; } = RepairStatus.Open;

    public decimal? Cost { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Indicates the consumer explicitly chose repair under the legal guarantee, unlocking the EU right-to-repair extension.
    /// </summary>
    public bool ConsumerOptedForRepair { get; set; }
}
