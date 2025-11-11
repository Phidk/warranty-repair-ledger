using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using WarrantyRepairLedger.Data;
using WarrantyRepairLedger.Dtos;
using WarrantyRepairLedger.Models;

namespace WarrantyRepairLedger.Endpoints;

public static class RepairEndpoints
{
    public static RouteGroupBuilder MapRepairEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/repairs");

        group.MapPost("/", CreateRepair);
        group.MapGet("/", GetRepairs);
        group.MapPatch("/{id:int}", UpdateRepairStatus);

        return group;
    }

    private static async Task<Results<Created<RepairResponse>, ValidationProblem, NotFound>> CreateRepair(
        RepairCreateRequest request,
        LedgerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = ValidateRepairCreate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var productExists = await dbContext.Products
            .AnyAsync(p => p.Id == request.ProductId, cancellationToken);

        if (!productExists)
        {
            return TypedResults.NotFound();
        }

        if (request.Status is not null && request.Status != RepairStatus.Open)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                { nameof(request.Status), ["New repairs must start in the Open status."] }
            });
        }

        var repair = new Repair
        {
            ProductId = request.ProductId,
            Status = RepairStatus.Open,
            OpenedAt = request.OpenedAt ?? DateTimeOffset.UtcNow,
            Cost = request.Cost,
            Notes = request.Notes?.Trim()
        };

        dbContext.Repairs.Add(repair);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/repairs/{repair.Id}", RepairResponse.FromEntity(repair));
    }

    private static async Task<Results<Ok<IEnumerable<RepairResponse>>, ValidationProblem>> GetRepairs(
        RepairStatus? status,
        LedgerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Repairs.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(r => r.Status == status);
        }

        var repairs = await query.ToListAsync(cancellationToken);

        var responses = repairs
            .OrderByDescending(r => r.OpenedAt)
            .Select(RepairResponse.FromEntity);
        return TypedResults.Ok(responses);
    }

    private static async Task<Results<Ok<RepairResponse>, ValidationProblem, NotFound>> UpdateRepairStatus(
        int id,
        RepairStatusUpdateRequest request,
        LedgerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var repair = await dbContext.Repairs
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (repair is null)
        {
            return TypedResults.NotFound();
        }

        if (repair.Status == request.Status)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                { nameof(request.Status), ["Repair is already in the requested status."] }
            });
        }

        if (!IsValidTransition(repair.Status, request.Status, out var reason))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                { nameof(request.Status), [reason] }
            });
        }

        repair.Status = request.Status;
        repair.ClosedAt = request.Status is RepairStatus.Fixed or RepairStatus.Rejected
            ? DateTimeOffset.UtcNow
            : null;

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(RepairResponse.FromEntity(repair));
    }

    private static Dictionary<string, string[]> ValidateRepairCreate(RepairCreateRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (request.ProductId <= 0)
        {
            errors[nameof(request.ProductId)] = ["ProductId must be provided."];
        }

        if (request.Cost is not null and < 0)
        {
            errors[nameof(request.Cost)] = ["Cost cannot be negative."];
        }

        return errors;
    }

    private static bool IsValidTransition(RepairStatus current, RepairStatus next, out string message)
    {
        message = string.Empty;

        if (current is RepairStatus.Fixed or RepairStatus.Rejected)
        {
            message = "Closed repairs cannot transition to a new status.";
            return false;
        }

        var allowed = (current, next) switch
        {
            (RepairStatus.Open, RepairStatus.InProgress) => true,
            (RepairStatus.InProgress, RepairStatus.Fixed) => true,
            (RepairStatus.InProgress, RepairStatus.Rejected) => true,
            _ => false
        };

        if (!allowed)
        {
            message = "Allowed transitions: Open -> InProgress -> Fixed|Rejected.";
        }

        return allowed;
    }
}
