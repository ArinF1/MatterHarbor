using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MatterHarbor.Infrastructure.Persistence;

public sealed class MatterHarborDbContextFactory : IDesignTimeDbContextFactory<MatterHarborDbContext>
{
    public MatterHarborDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("MATTERHARBOR_MIGRATION_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString =
                "Host=localhost;Port=5432;Database=matterharbor;Username=matterharbor;Password=matterharbor_dev";
        }

        var options = new DbContextOptionsBuilder<MatterHarborDbContext>()
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(MatterHarborDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "matterharbor");
            })
            .Options;

        return new MatterHarborDbContext(options);
    }
}
