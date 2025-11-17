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
}
