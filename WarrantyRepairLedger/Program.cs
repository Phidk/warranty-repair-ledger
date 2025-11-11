using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WarrantyRepairLedger.Data;
using WarrantyRepairLedger.Diagnostics;
using WarrantyRepairLedger.Endpoints;
using WarrantyRepairLedger.Options;
using WarrantyRepairLedger.Serialization;
using WarrantyRepairLedger.Services;

const string CorsPolicyName = "LedgerFrontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<WarrantyOptions>(
    builder.Configuration.GetSection(WarrantyOptions.SectionName));

builder.Services.AddSingleton<WarrantyEvaluator>();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        var traceId = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
        ctx.ProblemDetails.Extensions["traceId"] = traceId;
    };
});
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new DateOnlyJsonConverter());
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    var allowedOrigins = new[]
    {
        "http://localhost:5173",
        "https://localhost:5173",
        "http://127.0.0.1:5173",
        "http://localhost:4173",
        "https://localhost:4173",
        "http://127.0.0.1:4173",
    };

    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=./data/ledger.db";

builder.Services.AddDbContext<LedgerDbContext>(options =>
{
    var resolvedConnection = BuildConnectionString(connectionString, builder.Environment.ContentRootPath);
    options.UseSqlite(resolvedConnection);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/diagnostics/throw", (HttpContext _) =>
        throw new InvalidOperationException("Simulated failure for diagnostics."))
        .ExcludeFromDescription();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors(CorsPolicyName);

app.MapProductEndpoints();
app.MapRepairEndpoints();
app.MapReportEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
    db.Database.Migrate();
}

app.Run();

static string BuildConnectionString(string configuredConnectionString, string contentRoot)
{
    var builder = new SqliteConnectionStringBuilder(configuredConnectionString);
    if (!Path.IsPathRooted(builder.DataSource))
    {
        builder.DataSource = Path.GetFullPath(builder.DataSource, contentRoot);
    }

    var dataDirectory = Path.GetDirectoryName(builder.DataSource);
    if (!string.IsNullOrWhiteSpace(dataDirectory))
    {
        Directory.CreateDirectory(dataDirectory);
    }

    return builder.ToString();
}

public partial class Program;
