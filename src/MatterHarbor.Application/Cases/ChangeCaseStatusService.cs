using MatterHarbor.Application.Abstractions;

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
        item.ChangeStatus(command.Status, command.ExpectedVersion, clock.UtcNow);
        await store.SaveChangesAsync(cancellationToken);
        return CaseResponse.From(item);
    }
}
