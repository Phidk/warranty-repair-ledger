using WarrantyRepairLedger.Models;

namespace WarrantyRepairLedger.Dtos;

public record RepairCreateRequest(
    int ProductId,
    DateTimeOffset? OpenedAt,
    RepairStatus? Status,
    decimal? Cost,
    string? Notes);

public record RepairStatusUpdateRequest(RepairStatus Status);

public record RepairResponse(
    int Id,
    int ProductId,
    RepairStatus Status,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    decimal? Cost,
    string? Notes)
{
    public static RepairResponse FromEntity(Repair repair) =>
        new(
            repair.Id,
            repair.ProductId,
            repair.Status,
            repair.OpenedAt,
            repair.ClosedAt,
            repair.Cost,
            repair.Notes);
}

public record SummaryReportResponse(
    IDictionary<string, int> CountsByStatus,
    double? AverageDaysOpen,
    int ExpiringProducts);
