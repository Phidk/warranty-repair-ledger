using System.Data.Common;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WarrantyRepairLedger.Data;

namespace WarrantyRepairLedger.Tests;

    public class LedgerApiFactory : WebApplicationFactory<Program>
    {
        private DbConnection? _connection;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Replace the default DbContext with an in-memory SQLite connection so tests run isolated
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<LedgerDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton(provider =>
            {
                _connection ??= new SqliteConnection("Filename=:memory:");
                _connection.Open();
                return _connection;
            });

            services.AddDbContext<LedgerDbContext>((provider, options) =>
            {
                var connection = provider.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
            _connection = null;
        }
    }
}
