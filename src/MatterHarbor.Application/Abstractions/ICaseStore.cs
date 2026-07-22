using MatterHarbor.Application.Cases;
using MatterHarbor.Domain.Auditing;
using MatterHarbor.Domain.Cases;

namespace MatterHarbor.Application.Abstractions;

public interface ICaseStore
{
    Task<IStoreTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    Task AcquireIdempotencyLockAsync(Guid organizationId, string key, CancellationToken cancellationToken);

    Task<IdempotencySnapshot?> FindIdempotencyAsync(
        Guid organizationId,
        string key,
        CancellationToken cancellationToken);

    Task<bool> UserBelongsToOrganizationAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken);

    void AddCase(CaseItem caseItem);

    void AddAudit(AuditEntry auditEntry);

    void AddIdempotency(IdempotencyData idempotency);

    void AddOutbox(OutboxData outbox);

    Task<IReadOnlyList<CaseItem>> ListCasesAsync(
        Guid organizationId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<CaseItem?> FindCaseAsync(Guid organizationId, Guid caseId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IStoreTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}

public sealed record IdempotencySnapshot(string RequestHash, string ResponseJson);

public sealed record IdempotencyData(
    Guid OrganizationId,
    string Key,
    string RequestHash,
    string ResponseJson,
    DateTimeOffset CreatedAt);

public sealed record OutboxData(
    Guid Id,
    Guid OrganizationId,
    string Type,
    string Payload,
    DateTimeOffset OccurredAt);
