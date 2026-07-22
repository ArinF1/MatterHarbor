using MatterHarbor.Domain.Cases;

namespace MatterHarbor.Application.Cases;

public sealed record UserContext(Guid UserId, Guid OrganizationId);

public sealed record CreateCaseCommand(
    string Title,
    string Description,
    CasePriority Priority,
    Guid? AssignedUserId);

public sealed record CaseResponse(
    Guid Id,
    string CaseNumber,
    string Title,
    string Description,
    CasePriority Priority,
    CaseStatus Status,
    Guid? AssignedUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Version)
{
    public static CaseResponse From(CaseItem item) => new(
        item.Id,
        item.CaseNumber,
        item.Title,
        item.Description,
        item.Priority,
        item.Status,
        item.AssignedUserId,
        item.CreatedAt,
        item.UpdatedAt,
        item.Version);
}

public sealed record CreateCaseResult(CaseResponse Case, bool IsReplay);

public sealed record ChangeCaseStatusCommand(CaseStatus Status, int ExpectedVersion);
