using MatterHarbor.Application.Abstractions;
using MatterHarbor.Domain.Auditing;

namespace MatterHarbor.Application.Cases;

public sealed class ChangeCaseStatusService(ICaseStore store, IClock clock)
{
    public async Task<CaseResponse> ExecuteAsync(
        UserContext user,
        Guid caseId,
        ChangeCaseStatusCommand command,
        CancellationToken cancellationToken)
    {
        var item = await store.FindCaseAsync(user.OrganizationId, caseId, cancellationToken)
            ?? throw new CaseNotFoundException();
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
        await store.SaveChangesAsync(cancellationToken);
        return CaseResponse.From(item);
    }
}
