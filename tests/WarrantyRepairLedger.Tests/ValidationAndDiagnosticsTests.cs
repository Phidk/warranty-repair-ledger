using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WarrantyRepairLedger.Dtos;

namespace WarrantyRepairLedger.Tests;

public class ValidationAndDiagnosticsTests : IntegrationTestBase
{
    public ValidationAndDiagnosticsTests(LedgerApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateProduct_WithInvalidPayload_ReturnsValidationProblem()
    {
        var payload = new ProductCreateRequest(
            Name: string.Empty,
            Serial: string.Empty,
            PurchaseDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            WarrantyMonths: 0,
            Brand: null,
            Retailer: null,
            Price: -5);

        var response = await Client.PostAsJsonAsync("/products", payload, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem!.Status);
        Assert.True(problem.Errors.ContainsKey(nameof(payload.Name)));
        Assert.True(problem.Errors.ContainsKey(nameof(payload.Serial)));
        Assert.True(problem.Errors.ContainsKey(nameof(payload.WarrantyMonths)));
        Assert.True(problem.Errors.ContainsKey(nameof(payload.Price)));
    }

    [Fact]
    public async Task DiagnosticsEndpoint_ReturnsProblemDetailsWithTraceId()
    {
        var response = await Client.GetAsync("/diagnostics/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem!.Status);
        Assert.Equal("An unexpected error occurred.", problem.Title);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }
}
