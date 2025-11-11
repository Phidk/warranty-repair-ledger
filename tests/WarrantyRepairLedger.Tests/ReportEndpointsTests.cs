using System.Net.Http.Json;
using WarrantyRepairLedger.Dtos;

namespace WarrantyRepairLedger.Tests;

public class ReportEndpointsTests : IntegrationTestBase
{
    public ReportEndpointsTests(LedgerApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SummaryReflectsOpenRepairsAndExpiringProducts()
    {
        var product = await CreateProductAsync(new ProductCreateRequest(
            Name: "Legacy Laptop",
            Serial: "SN-" + Guid.NewGuid(),
            PurchaseDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-23)),
            WarrantyMonths: 24,
            Brand: "Acme",
            Retailer: "Shop",
            Price: 799));

        await CreateRepairAsync(product.Id);

        var summary = await Client.GetFromJsonAsync<SummaryReportResponse>("/reports/summary");

        Assert.NotNull(summary);
        Assert.True(summary!.CountsByStatus.TryGetValue("Open", out var openCount));
        Assert.Equal(1, openCount);
        Assert.True(summary.ExpiringProducts >= 1);
        Assert.NotNull(summary.AverageDaysOpen);
    }
}
