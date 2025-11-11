using System.Net.Http.Json;
using WarrantyRepairLedger.Dtos;
using WarrantyRepairLedger.Models;
using Xunit;

namespace WarrantyRepairLedger.Tests;

public abstract class IntegrationTestBase : IClassFixture<LedgerApiFactory>, IAsyncLifetime
{
    protected LedgerApiFactory Factory { get; }
    protected HttpClient Client { get; private set; } = null!;

    protected IntegrationTestBase(LedgerApiFactory factory)
    {
        Factory = factory;
    }

    public virtual async Task InitializeAsync()
    {
        Client = Factory.CreateClient();
        await Factory.ResetDatabaseAsync();
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;

    // Helper that hides the boilerplate payloads for most product scenarios
    protected async Task<ProductResponse> CreateProductAsync(ProductCreateRequest? request = null)
    {
        var payload = request ?? new ProductCreateRequest(
            Name: "Phone X",
            Serial: Guid.NewGuid().ToString("N"),
            PurchaseDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3)),
            WarrantyMonths: 24,
            Brand: "Acme",
            Retailer: "Example Store",
            Price: 499.99m);

        var response = await Client.PostAsJsonAsync("/products", payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    // Opens a repair tied to an existing product for integration flows
    protected async Task<RepairResponse> CreateRepairAsync(int productId, RepairStatus status = RepairStatus.Open)
    {
        var request = new RepairCreateRequest(
            ProductId: productId,
            OpenedAt: DateTimeOffset.UtcNow,
            Status: status,
            Cost: null,
            Notes: "Screen flicker");

        var response = await Client.PostAsJsonAsync("/repairs", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RepairResponse>())!;
    }
}
