using MatterHarbor.Domain.Cases;

namespace MatterHarbor.Api.Contracts;

public sealed record CreateCaseRequest(
    string Title,
    string Description,
    CasePriority Priority,
    Guid? AssignedUserId);

public sealed record ChangeCaseStatusRequest(CaseStatus Status, int Version);
