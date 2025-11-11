using System.Net;
using System.Net.Http.Json;
using WarrantyRepairLedger.Dtos;
using WarrantyRepairLedger.Models;

namespace WarrantyRepairLedger.Tests;

public class RepairEndpointsTests : IntegrationTestBase
{
    public RepairEndpointsTests(LedgerApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task PatchRepair_DisallowsSkippingStates()
    {
        var product = await CreateProductAsync();
        var repair = await CreateRepairAsync(product.Id);

        var response = await Client.PatchAsJsonAsync($"/repairs/{repair.Id}",
            new RepairStatusUpdateRequest(RepairStatus.Fixed));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchRepair_AllowsSequentialFlow()
    {
        var product = await CreateProductAsync();
        var repair = await CreateRepairAsync(product.Id);

        var toInProgress = await Client.PatchAsJsonAsync($"/repairs/{repair.Id}",
            new RepairStatusUpdateRequest(RepairStatus.InProgress));
        toInProgress.EnsureSuccessStatusCode();

        var updated = await toInProgress.Content.ReadFromJsonAsync<RepairResponse>();
        Assert.NotNull(updated);
        Assert.Equal(RepairStatus.InProgress, updated!.Status);

        var toFixed = await Client.PatchAsJsonAsync($"/repairs/{repair.Id}",
            new RepairStatusUpdateRequest(RepairStatus.Fixed));
        toFixed.EnsureSuccessStatusCode();

        var closed = await toFixed.Content.ReadFromJsonAsync<RepairResponse>();
        Assert.NotNull(closed);
        Assert.Equal(RepairStatus.Fixed, closed!.Status);
        Assert.NotNull(closed.ClosedAt);
    }
}
