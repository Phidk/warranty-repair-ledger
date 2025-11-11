using System.ComponentModel.DataAnnotations;
using WarrantyRepairLedger.Models;

namespace WarrantyRepairLedger.Dtos;

public record ProductCreateRequest(
    [property: Required(AllowEmptyStrings = false, ErrorMessage = "Name is required.")]
    string Name,

    [property: Required(AllowEmptyStrings = false, ErrorMessage = "Serial is required.")]
    string Serial,

    [property: Required(ErrorMessage = "Purchase date is required.")]
    DateOnly PurchaseDate,

    [property: Range(1, int.MaxValue, ErrorMessage = "Warranty months must be positive.")]
    int? WarrantyMonths,

    string? Brand,
    string? Retailer,

    [property: Range(0, double.MaxValue, ErrorMessage = "Price cannot be negative.")]
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
