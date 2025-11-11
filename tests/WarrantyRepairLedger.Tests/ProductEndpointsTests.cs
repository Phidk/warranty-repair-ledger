using System.Net.Http.Json;
using WarrantyRepairLedger.Dtos;

namespace WarrantyRepairLedger.Tests;

public class ProductEndpointsTests : IntegrationTestBase
{
    public ProductEndpointsTests(LedgerApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetExpiringProducts_ReturnsProductsWithinWindow()
    {
        await CreateProductAsync(new ProductCreateRequest(
            Name: "Almost Done",
            Serial: "SN-" + Guid.NewGuid(),
            PurchaseDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-23)),
            WarrantyMonths: 24,
            Brand: "Acme",
            Retailer: "Shop",
            Price: null));

        await CreateProductAsync(new ProductCreateRequest(
            Name: "Still Fresh",
            Serial: "SN-" + Guid.NewGuid(),
            PurchaseDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-5)),
            WarrantyMonths: 24,
            Brand: "Acme",
            Retailer: "Shop",
            Price: null));

        await CreateProductAsync(new ProductCreateRequest(
            Name: "Short Warranty",
            Serial: "SN-" + Guid.NewGuid(),
            PurchaseDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-11)),
            WarrantyMonths: 12,
            Brand: "Acme",
            Retailer: "Shop",
            Price: null));

        var expiring = await Client.GetFromJsonAsync<List<ExpiringProductResponse>>("/products/expiring?days=60", JsonOptions);

        Assert.NotNull(expiring);
        Assert.Equal(2, expiring!.Count);
        Assert.All(expiring, item => Assert.True(item.DaysRemaining <= 60));
    }
}
