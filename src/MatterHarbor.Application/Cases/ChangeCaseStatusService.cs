using MatterHarbor.Application.Abstractions;
using MatterHarbor.Domain.Auditing;

namespace MatterHarbor.Application.Cases;

public sealed class ChangeCaseStatusService(ICaseStore store, IClock clock)
{
    public async Task<CaseMutationResult> ExecuteAsync(
        UserContext user,
        Guid caseId,
        string idempotencyKey,
        ChangeCaseStatusCommand command,
        CancellationToken cancellationToken)
    {
        var role = await CaseAuthorization.RequireMemberAsync(store, user, cancellationToken);
        var storeKey = CaseMutationIdempotency.StoreKey("case.status", user, caseId, idempotencyKey);
        var requestHash = CaseMutationIdempotency.RequestHash(command);
        await using var transaction = await store.BeginTransactionAsync(cancellationToken);
        await store.AcquireIdempotencyLockAsync(user.OrganizationId, storeKey, cancellationToken);
        var item = await store.FindCaseAsync(user.OrganizationId, caseId, cancellationToken)
            ?? throw new CaseNotFoundException();
        CaseAuthorization.RequireStatusChange(role, user, item.AssignedUserId);
        var replay = await CaseMutationIdempotency.FindReplayAsync(
            store, user.OrganizationId, storeKey, requestHash, cancellationToken);
        if (replay is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new CaseMutationResult(replay, true);
        }

        var previousStatus = item.Status;
        var now = clock.UtcNow;
        item.ChangeStatus(command.Status, command.ExpectedVersion, now);
        store.AddAudit(new AuditEntry(
            Guid.NewGuid(),
            item.OrganizationId,
            user.UserId,
            item.Id,
            $"case.status.changed:{previousStatus}->{command.Status}",
            now));
        var response = CaseResponse.From(item) with { Version = command.ExpectedVersion + 1 };
        store.AddIdempotency(CaseMutationIdempotency.Record(user.OrganizationId, storeKey, requestHash, response, now));
        await store.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new CaseMutationResult(CaseResponse.From(item), false);
    }
}
