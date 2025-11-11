using WarrantyRepairLedger.Models;

namespace WarrantyRepairLedger.Dtos;

public record ProductCreateRequest(
    string Name,
    string Serial,
    DateOnly PurchaseDate,
    int? WarrantyMonths,
    string? Brand,
    string? Retailer,
    decimal? Price);

public record ProductResponse(
    int Id,
    string Name,
    string? Brand,
    string Serial,
    DateOnly PurchaseDate,
    int WarrantyMonths,
    string? Retailer,
    decimal? Price)
{
    public static ProductResponse FromEntity(Product product) =>
        new(
            product.Id,
            product.Name,
            product.Brand,
            product.Serial,
            product.PurchaseDate,
            product.WarrantyMonths,
            product.Retailer,
            product.Price);
}

public record ExpiringProductResponse(ProductResponse Product, int DaysRemaining);

public record WarrantyStatusResponse(bool InWarranty, DateOnly ExpiresOn, string Reason);
