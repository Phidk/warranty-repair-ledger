using System.Net;
using System.Net.Http.Json;
using WarrantyRepairLedger.Dtos;
using WarrantyRepairLedger.Models;

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

    [Fact]
    public async Task WarrantyStatus_ReflectsRightToRepairExtension()
    {
        var product = await CreateProductAsync(new ProductCreateRequest(
            Name: "Aged Phone",
            Serial: "SN-" + Guid.NewGuid(),
            PurchaseDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-30)),
            WarrantyMonths: 24,
            Brand: "Acme",
            Retailer: "Shop",
            Price: null));

        var repair = await CreateRepairAsync(product.Id, consumerOptedForRepair: true);

        await Client.PatchAsJsonAsync($"/repairs/{repair.Id}",
            new RepairStatusUpdateRequest(RepairStatus.InProgress), JsonOptions);
        var closure = await Client.PatchAsJsonAsync($"/repairs/{repair.Id}",
            new RepairStatusUpdateRequest(RepairStatus.Fixed), JsonOptions);
        closure.EnsureSuccessStatusCode();

        var status = await Client.GetFromJsonAsync<WarrantyStatusResponse>(
            $"/products/{product.Id}/in-warranty", JsonOptions);

        Assert.NotNull(status);
        Assert.True(status!.InWarranty);
        var minExpected = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(12);
        Assert.True(status.ExpiresOn >= minExpected);
    }

    [Fact]
    public async Task DeleteProduct_RemovesProductAndAssociatedRepairs()
    {
        var product = await CreateProductAsync();
        var repair = await CreateRepairAsync(product.Id);

        var deleteResponse = await Client.DeleteAsync($"/products/{product.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var productResponse = await Client.GetAsync($"/products/{product.Id}");
        Assert.Equal(HttpStatusCode.NotFound, productResponse.StatusCode);

        var repairs = await Client.GetFromJsonAsync<List<RepairResponse>>("/repairs", JsonOptions);
        Assert.NotNull(repairs);
        Assert.DoesNotContain(repairs!, r => r.Id == repair.Id);
    }
}
