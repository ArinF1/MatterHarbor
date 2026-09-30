using MatterHarbor.Application.Abstractions;
using MatterHarbor.Domain.Auditing;
using MatterHarbor.Domain.Organizations;

namespace MatterHarbor.Application.Cases;

public sealed class ChangeCaseAssignmentService(ICaseStore store, IClock clock)
{
    public async Task<CaseMutationResult> ExecuteAsync(
        UserContext user,
        Guid caseId,
        string idempotencyKey,
        ChangeCaseAssignmentCommand command,
        CancellationToken cancellationToken)
    {
        var role = await CaseAuthorization.RequireMemberAsync(store, user, cancellationToken);
        CaseAuthorization.RequireAssignment(role);
        var storeKey = CaseMutationIdempotency.StoreKey("case.assignment", user, caseId, idempotencyKey);
        var requestHash = CaseMutationIdempotency.RequestHash(command);
        await using var transaction = await store.BeginTransactionAsync(cancellationToken);
        await store.AcquireIdempotencyLockAsync(user.OrganizationId, storeKey, cancellationToken);
        var item = await store.FindCaseAsync(user.OrganizationId, caseId, cancellationToken)
            ?? throw new CaseNotFoundException();

        var replay = await CaseMutationIdempotency.FindReplayAsync(
            store, user.OrganizationId, storeKey, requestHash, cancellationToken);
        if (replay is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new CaseMutationResult(replay, true);
        }

        if (command.AssignedUserId is { } assignedUserId &&
            await store.GetUserRoleAsync(user.OrganizationId, assignedUserId, cancellationToken) is not
                (OrganizationRole.Administrator or OrganizationRole.CaseWorker))
        {
            throw new AssignedUserNotFoundException();
        }

        var now = clock.UtcNow;
        item.Assign(command.AssignedUserId, command.ExpectedVersion, now);
        store.AddAudit(new AuditEntry(
            Guid.NewGuid(), user.OrganizationId, user.UserId, caseId, "case.assignment.changed", now));
        var response = CaseResponse.From(item) with { Version = command.ExpectedVersion + 1 };
        store.AddIdempotency(CaseMutationIdempotency.Record(user.OrganizationId, storeKey, requestHash, response, now));
        await store.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new CaseMutationResult(CaseResponse.From(item), false);
    }
}
