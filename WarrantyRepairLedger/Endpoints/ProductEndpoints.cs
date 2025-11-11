using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WarrantyRepairLedger.Data;
using WarrantyRepairLedger.Dtos;
using WarrantyRepairLedger.Models;
using WarrantyRepairLedger.Options;
using WarrantyRepairLedger.Services;

namespace WarrantyRepairLedger.Endpoints;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/products");

        group.MapPost("/", CreateProduct);
        group.MapGet("/", GetProducts);
        group.MapGet("/{id:int}", GetProduct);
        group.MapGet("/{id:int}/in-warranty", GetWarrantyStatus);
        group.MapGet("/expiring", GetExpiringProducts);

        return group;
    }

    private static async Task<Results<Created<ProductResponse>, ValidationProblem>> CreateProduct(
        ProductCreateRequest request,
        LedgerDbContext dbContext,
        IOptions<WarrantyOptions> warrantyOptions,
        CancellationToken cancellationToken) 
    {
        var errors = ValidateProductRequest(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var normalizedSerial = request.Serial.Trim();
        var existingSerial = await dbContext.Products
            .AnyAsync(p => p.Serial == normalizedSerial, cancellationToken);

        if (existingSerial)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                { nameof(request.Serial), ["Serial must be unique."] }
            });
        }

        var normalizedName = request.Name.Trim();
        var warrantyMonths = GetWarrantyMonths(request, warrantyOptions.Value);

        var product = new Product
        {
            Name = normalizedName,
            Brand = request.Brand?.Trim(),
            Serial = normalizedSerial,
            PurchaseDate = request.PurchaseDate,
            WarrantyMonths = warrantyMonths,
            Retailer = request.Retailer?.Trim(),
            Price = request.Price
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/products/{product.Id}", ProductResponse.FromEntity(product));
    }

    private static async Task<Results<Ok<IEnumerable<ProductResponse>>, ValidationProblem>> GetProducts(
        string? q,
        LedgerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var like = $"%{q.Trim()}%";
            query = query.Where(p =>
                EF.Functions.Like(p.Name, like) ||
                EF.Functions.Like(p.Serial, like));
        }

        var products = await query
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var responses = products.Select(ProductResponse.FromEntity);

        return TypedResults.Ok(responses);
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound>> GetProduct(
        int id,
        LedgerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return product is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ProductResponse.FromEntity(product));
    }

    private static async Task<Results<Ok<WarrantyStatusResponse>, NotFound>> GetWarrantyStatus(
        int id,
        LedgerDbContext dbContext,
        WarrantyEvaluator evaluator,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            return TypedResults.NotFound();
        }

        var window = evaluator.Evaluate(product);
        var response = new WarrantyStatusResponse(window.InWarranty, window.ExpiresOn, window.Reason);

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<IEnumerable<ExpiringProductResponse>>, ValidationProblem>> GetExpiringProducts(
        int? days,
        LedgerDbContext dbContext,
        WarrantyEvaluator evaluator,
        CancellationToken cancellationToken)
    {
        var threshold = days ?? 30;

        if (threshold <= 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                { nameof(days), ["Days must be greater than zero."] }
            });
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var products = await dbContext.Products
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var expiring = products
            .Where(p => evaluator.IsExpiringWithin(p, threshold, today))
            .Select(p =>
            {
                var expiresOn = evaluator.GetExpirationDate(p);
                var remaining = Math.Max(0, expiresOn.DayNumber - today.DayNumber);
                return new ExpiringProductResponse(ProductResponse.FromEntity(p), remaining);
            })
            .OrderBy(x => x.DaysRemaining)
            .ThenBy(x => x.Product.Name)
            .ToList();

        return TypedResults.Ok<IEnumerable<ExpiringProductResponse>>(expiring);
    }

    private static Dictionary<string, string[]> ValidateProductRequest(ProductCreateRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = ["Name is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Serial))
        {
            errors[nameof(request.Serial)] = ["Serial is required."];
        }

        if (request.PurchaseDate == default)
        {
            errors[nameof(request.PurchaseDate)] = ["Purchase date is required."];
        }

        if (request.WarrantyMonths is not null and <= 0)
        {
            errors[nameof(request.WarrantyMonths)] = ["Warranty months must be positive."];
        }

        return errors;
    }

    private static int GetWarrantyMonths(ProductCreateRequest request, WarrantyOptions options)
    {
        if (request.WarrantyMonths is null)
        {
            return options.DefaultMonths;
        }

        return request.WarrantyMonths.Value > 0
            ? request.WarrantyMonths.Value
            : options.DefaultMonths;
    }
}
