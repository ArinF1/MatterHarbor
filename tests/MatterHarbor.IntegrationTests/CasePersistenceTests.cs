using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MatterHarbor.Application.Abstractions;
using MatterHarbor.Application.Cases;
using MatterHarbor.Domain.Cases;
using MatterHarbor.Infrastructure.Messaging;
using MatterHarbor.Infrastructure.Persistence;

namespace MatterHarbor.IntegrationTests;

public sealed class CasePersistenceTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_writes_case_audit_and_outbox_atomically()
    {
        var tenant = await fixture.AddTenantAsync();
        await using var context = fixture.CreateContext();
        var service = CreateService(context);

        var result = await service.ExecuteAsync(
            new UserContext(tenant.UserId, tenant.OrganizationId),
            "create-1",
            ValidCommand(),
            CancellationToken.None);

        Assert.False(result.IsReplay);
        Assert.Equal(1, await context.Cases.CountAsync(x => x.OrganizationId == tenant.OrganizationId));
        Assert.Equal(1, await context.AuditEntries.CountAsync(x => x.EntityId == result.Case.Id));
        Assert.Equal(1, await context.OutboxMessages.CountAsync(x => x.OrganizationId == tenant.OrganizationId));
    }

    [Fact]
    public async Task Duplicate_idempotent_request_creates_one_case()
    {
        var tenant = await fixture.AddTenantAsync();
        await using var context = fixture.CreateContext();
        var service = CreateService(context);
        var user = new UserContext(tenant.UserId, tenant.OrganizationId);

        var first = await service.ExecuteAsync(user, "duplicate-key", ValidCommand(), CancellationToken.None);
        var second = await service.ExecuteAsync(user, "duplicate-key", ValidCommand(), CancellationToken.None);

        Assert.Equal(first.Case.Id, second.Case.Id);
        Assert.True(second.IsReplay);
        Assert.Equal(1, await context.Cases.CountAsync(x => x.OrganizationId == tenant.OrganizationId));
    }

    [Fact]
    public async Task Reusing_key_with_different_input_returns_conflict()
    {
        var tenant = await fixture.AddTenantAsync();
        await using var context = fixture.CreateContext();
        var service = CreateService(context);
        var user = new UserContext(tenant.UserId, tenant.OrganizationId);
        await service.ExecuteAsync(user, "conflict-key", ValidCommand(), CancellationToken.None);

        await Assert.ThrowsAsync<IdempotencyConflictException>(() => service.ExecuteAsync(
            user,
            "conflict-key",
            ValidCommand() with { Title = "Different title" },
            CancellationToken.None));
    }

    [Fact]
    public async Task Organization_isolation_hides_foreign_case()
    {
        var firstTenant = await fixture.AddTenantAsync();
        var secondTenant = await fixture.AddTenantAsync();
        await using var context = fixture.CreateContext();
        var created = await CreateService(context).ExecuteAsync(
            new UserContext(firstTenant.UserId, firstTenant.OrganizationId),
            "isolated-key",
            ValidCommand(),
            CancellationToken.None);
        var query = new CaseQueryService(new CaseStore(context));

        await Assert.ThrowsAsync<CaseNotFoundException>(() => query.GetAsync(
            new UserContext(secondTenant.UserId, secondTenant.OrganizationId),
            created.Case.Id,
            CancellationToken.None));
    }

    [Fact]
    public async Task Stale_database_update_detects_optimistic_concurrency_conflict()
    {
        var tenant = await fixture.AddTenantAsync();
        Guid caseId;
        await using (var createContext = fixture.CreateContext())
        {
            var created = await CreateService(createContext).ExecuteAsync(
                new UserContext(tenant.UserId, tenant.OrganizationId),
                "concurrency-key",
                ValidCommand(),
                CancellationToken.None);
            caseId = created.Case.Id;
        }

        await using var firstContext = fixture.CreateContext();
        await using var secondContext = fixture.CreateContext();
        var firstStore = new CaseStore(firstContext);
        var secondStore = new CaseStore(secondContext);
        var first = await firstStore.FindCaseAsync(tenant.OrganizationId, caseId, CancellationToken.None);
        var second = await secondStore.FindCaseAsync(tenant.OrganizationId, caseId, CancellationToken.None);
        Assert.NotNull(first);
        Assert.NotNull(second);

        first.ChangeStatus(CaseStatus.InProgress, 1, Now.AddMinutes(1));
        await firstStore.SaveChangesAsync(CancellationToken.None);
        second.ChangeStatus(CaseStatus.Resolved, 1, Now.AddMinutes(2));

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            secondStore.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Concurrent_workers_publish_an_outbox_record_once()
    {
        await using (var cleanupContext = fixture.CreateContext())
        {
            await cleanupContext.OutboxMessages.ExecuteDeleteAsync();
        }

        var tenant = await fixture.AddTenantAsync();
        await using (var createContext = fixture.CreateContext())
        {
            await CreateService(createContext).ExecuteAsync(
                new UserContext(tenant.UserId, tenant.OrganizationId),
                "worker-key",
                ValidCommand(),
                CancellationToken.None);
        }

        var publisher = new CountingPublisher();
        await using var firstContext = fixture.CreateContext();
        await using var secondContext = fixture.CreateContext();
        var first = CreateProcessor(firstContext, publisher);
        var second = CreateProcessor(secondContext, publisher);

        await Task.WhenAll(
            first.ProcessBatchAsync(100, CancellationToken.None),
            second.ProcessBatchAsync(100, CancellationToken.None));

        Assert.Equal(1, publisher.Count);
    }

    [Fact]
    public async Task Development_seed_is_idempotent_in_a_nonempty_database()
    {
        await fixture.AddTenantAsync();
        var services = new ServiceCollection();
        services.AddDbContext<MatterHarborDbContext>(options => options.UseNpgsql(
            fixture.ConnectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "matterharbor")));
        await using var provider = services.BuildServiceProvider();

        await provider.InitializeDevelopmentMatterHarborDatabaseAsync();
        await provider.InitializeDevelopmentMatterHarborDatabaseAsync();

        await using var context = fixture.CreateContext();
        Assert.Equal(1, await context.Organizations.CountAsync(
            x => x.Id == DatabaseInitialization.NorthwindOrganizationId));
        Assert.Equal(1, await context.OrganizationUsers.CountAsync(
            x => x.Id == DatabaseInitialization.AlexUserId));
    }

    private static CreateCaseCommand ValidCommand() => new(
        "Broken streetlight",
        "Lamp outside the library is dark.",
        CasePriority.High,
        null);

    private static CreateCaseService CreateService(MatterHarborDbContext context) =>
        new(new CaseStore(context), new FixedClock(Now));

    private static OutboxProcessor CreateProcessor(MatterHarborDbContext context, IOutboxPublisher publisher) =>
        new(context, publisher, new FixedClock(Now), NullLogger<OutboxProcessor>.Instance);

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class CountingPublisher : IOutboxPublisher
    {
        private int count;

        public int Count => count;

        public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref count);
            await Task.Delay(50, cancellationToken);
        }
    }
}
