using System.ComponentModel.DataAnnotations;
using WarrantyRepairLedger.Models;

namespace WarrantyRepairLedger.Dtos;

public record RepairCreateRequest(
    [property: Range(1, int.MaxValue, ErrorMessage = "ProductId must be provided.")]
    int ProductId,
    DateTimeOffset? OpenedAt,
    RepairStatus? Status,
    [property: Range(0, double.MaxValue, ErrorMessage = "Cost cannot be negative.")]
    decimal? Cost,
    string? Notes,
    bool ConsumerOptedForRepair = false);

public record RepairStatusUpdateRequest(
    [property: Required(ErrorMessage = "Status is required.")]
    RepairStatus Status);

public record RepairResponse(
    int Id,
    int ProductId,
    RepairStatus Status,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    decimal? Cost,
    string? Notes,
    bool ConsumerOptedForRepair)
{
    public static RepairResponse FromEntity(Repair repair) =>
        new(
            repair.Id,
            repair.ProductId,
            repair.Status,
            repair.OpenedAt,
            repair.ClosedAt,
            repair.Cost,
            repair.Notes,
            repair.ConsumerOptedForRepair);
}

public record SummaryReportResponse(
    IDictionary<string, int> CountsByStatus,
    double? AverageDaysOpen,
    int ExpiringProducts);
