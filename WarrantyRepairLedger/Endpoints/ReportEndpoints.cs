using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using WarrantyRepairLedger.Data;
using WarrantyRepairLedger.Dtos;
using WarrantyRepairLedger.Models;
using WarrantyRepairLedger.Services;

namespace WarrantyRepairLedger.Endpoints;

public static class ReportEndpoints
{
    public static RouteGroupBuilder MapReportEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/reports");
        group.MapGet("/summary", GetSummary);
        return group;
    }

    private static async Task<Ok<SummaryReportResponse>> GetSummary(
        LedgerDbContext dbContext,
        WarrantyEvaluator evaluator,
        CancellationToken cancellationToken)
    {
        // Pull all repairs for lightweight aggregation (counts and average open days)
        var repairs = await dbContext.Repairs
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var counts = Enum
            .GetValues<RepairStatus>()
            .ToDictionary(
                status => status.ToString(),
                status => repairs.Count(r => r.Status == status));

        double? averageDaysOpen = null;
        if (repairs.Count > 0)
        {
            averageDaysOpen = repairs
                .Select(r => ((r.ClosedAt ?? DateTimeOffset.UtcNow) - r.OpenedAt).TotalDays)
                .Average();
        }

        var products = await dbContext.Products
            .AsNoTracking()
            .Include(p => p.Repairs)
            .ToListAsync(cancellationToken);

        var expiringSoon = products.Count(p => evaluator.IsExpiringWithin(p, 30, repairs: p.Repairs));

        var response = new SummaryReportResponse(counts, averageDaysOpen, expiringSoon);
        return TypedResults.Ok(response);
    }
}
