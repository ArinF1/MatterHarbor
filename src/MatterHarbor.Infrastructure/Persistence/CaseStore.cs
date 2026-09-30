using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MatterHarbor.Application.Abstractions;
using MatterHarbor.Application.Cases;
using MatterHarbor.Domain.Auditing;
using MatterHarbor.Domain.Cases;
using MatterHarbor.Domain.Organizations;

namespace MatterHarbor.Infrastructure.Persistence;

public sealed class CaseStore(MatterHarborDbContext dbContext) : ICaseStore
{
    public async Task<IStoreTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new StoreTransaction(transaction);
    }

    public Task AcquireIdempotencyLockAsync(
        Guid organizationId,
        string key,
        CancellationToken cancellationToken)
    {
        var lockName = $"{organizationId:N}:{key}";
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockName}, 0))",
            cancellationToken);
    }

    public async Task<IdempotencySnapshot?> FindIdempotencyAsync(
        Guid organizationId,
        string key,
        CancellationToken cancellationToken)
    {
        return await dbContext.IdempotencyRecords
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.Key == key)
            .Select(x => new IdempotencySnapshot(x.RequestHash, x.ResponseJson))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<OrganizationRole?> GetUserRoleAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.OrganizationUsers
            .Where(x => x.OrganizationId == organizationId && x.Id == userId)
            .Select(x => (OrganizationRole?)x.Role)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CaseAssigneeResponse>> ListAssigneesAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        return await dbContext.OrganizationUsers
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId &&
                (x.Role == OrganizationRole.Administrator || x.Role == OrganizationRole.CaseWorker))
            .OrderBy(x => x.DisplayName)
            .Select(x => new CaseAssigneeResponse(x.Id, x.DisplayName))
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public void AddCase(CaseItem caseItem) => dbContext.Cases.Add(caseItem);

    public void AddAudit(AuditEntry auditEntry) => dbContext.AuditEntries.Add(auditEntry);

    public void AddIdempotency(IdempotencyData idempotency) => dbContext.IdempotencyRecords.Add(new IdempotencyRecord
    {
        OrganizationId = idempotency.OrganizationId,
        Key = idempotency.Key,
        RequestHash = idempotency.RequestHash,
        ResponseJson = idempotency.ResponseJson,
        CreatedAt = idempotency.CreatedAt
    });

    public void AddOutbox(OutboxData outbox) => dbContext.OutboxMessages.Add(new OutboxMessage
    {
        Id = outbox.Id,
        OrganizationId = outbox.OrganizationId,
        Type = outbox.Type,
        Payload = outbox.Payload,
        OccurredAt = outbox.OccurredAt
    });

    public async Task<IReadOnlyList<CaseItem>> ListCasesAsync(
        Guid organizationId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        return await dbContext.Cases
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<CaseItem?> FindCaseAsync(Guid organizationId, Guid caseId, CancellationToken cancellationToken)
    {
        return dbContext.Cases.SingleOrDefaultAsync(
            x => x.OrganizationId == organizationId && x.Id == caseId,
            cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(exception);
        }
    }

    private sealed class StoreTransaction(IDbContextTransaction transaction) : IStoreTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
