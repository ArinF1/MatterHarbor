using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MatterHarbor.Infrastructure.Persistence;

namespace MatterHarbor.IntegrationTests;

public sealed class ApiWebApplicationFactory(
    string connectionString,
    int rateLimitPermitLimit = 120) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting(
            "RateLimiting:PermitLimit",
            rateLimitPermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("RateLimiting:WindowSeconds", "3600");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<MatterHarborDbContext>>();
            services.AddDbContext<MatterHarborDbContext>(options =>
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(MatterHarborDbContext).Assembly.FullName);
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "matterharbor");
                }));
        });
    }
}
