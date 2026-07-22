using Microsoft.EntityFrameworkCore;
using MatterHarbor.Domain.Organizations;
using MatterHarbor.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace MatterHarbor.IntegrationTests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private PostgreSqlContainer? container;
    private string connectionString = string.Empty;

    public string ConnectionString => connectionString;

    public async Task InitializeAsync()
    {
        var externalConnection = Environment.GetEnvironmentVariable("MATTERHARBOR_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(externalConnection))
        {
            container = new PostgreSqlBuilder()
                .WithImage("postgres:17-alpine")
                .WithDatabase("matterharbor_tests")
                .WithUsername("matterharbor")
                .WithPassword("test_password")
                .Build();
            await container.StartAsync();
            connectionString = container.GetConnectionString();
        }
        else
        {
            connectionString = externalConnection;
        }

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }

    public MatterHarborDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MatterHarborDbContext>()
            .UseNpgsql(ConnectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "matterharbor"))
            .Options;
        return new MatterHarborDbContext(options);
    }

    public async Task<(Guid OrganizationId, Guid UserId)> AddTenantAsync()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        context.Organizations.Add(new Organization(organizationId, $"Organization {organizationId:N}"));
        context.OrganizationUsers.Add(new OrganizationUser(
            userId,
            organizationId,
            $"subject-{userId:N}",
            "Test User"));
        await context.SaveChangesAsync();
        return (organizationId, userId);
    }
}
