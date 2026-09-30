using MatterHarbor.Application.Abstractions;

namespace MatterHarbor.Application.Cases;

public sealed class CaseQueryService(ICaseStore store)
{
    public async Task<IReadOnlyList<CaseAssigneeResponse>> ListAssigneesAsync(
        UserContext user,
        CancellationToken cancellationToken)
    {
        var role = await CaseAuthorization.RequireMemberAsync(store, user, cancellationToken);
        CaseAuthorization.RequireAssignment(role);
        return await store.ListAssigneesAsync(user.OrganizationId, cancellationToken);
    }

    public async Task<IReadOnlyList<CaseResponse>> ListAsync(
        UserContext user,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await CaseAuthorization.RequireMemberAsync(store, user, cancellationToken);
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var cases = await store.ListCasesAsync(
            user.OrganizationId,
            (safePage - 1) * safePageSize,
            safePageSize,
            cancellationToken);
        return cases.Select(CaseResponse.From).ToArray();
    }

    public async Task<CaseResponse> GetAsync(
        UserContext user,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        await CaseAuthorization.RequireMemberAsync(store, user, cancellationToken);
        var item = await store.FindCaseAsync(user.OrganizationId, caseId, cancellationToken)
            ?? throw new CaseNotFoundException();
        return CaseResponse.From(item);
    }
}
