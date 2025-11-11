using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WarrantyRepairLedger.Data;
using WarrantyRepairLedger.Endpoints;
using WarrantyRepairLedger.Options;
using WarrantyRepairLedger.Serialization;
using WarrantyRepairLedger.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<WarrantyOptions>(
    builder.Configuration.GetSection(WarrantyOptions.SectionName));

builder.Services.AddSingleton<WarrantyEvaluator>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new DateOnlyJsonConverter());
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseHttpsRedirection();

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
