using MatterHarbor.Application.Abstractions;
using MatterHarbor.Domain.Organizations;

namespace MatterHarbor.Application.Cases;

public static class CaseAuthorization
{
    public static async Task<OrganizationRole> RequireMemberAsync(
        ICaseStore store,
        UserContext user,
        CancellationToken cancellationToken)
    {
        return await store.GetUserRoleAsync(user.OrganizationId, user.UserId, cancellationToken)
            ?? throw new CaseAccessDeniedException();
    }

    public static void RequireCreate(OrganizationRole role, UserContext user, Guid? assignedUserId)
    {
        if (role == OrganizationRole.Viewer ||
            (role == OrganizationRole.CaseWorker && assignedUserId is not null && assignedUserId != user.UserId))
        {
            throw new CaseAccessDeniedException();
        }
    }

    public static void RequireStatusChange(OrganizationRole role, UserContext user, Guid? assignedUserId)
    {
        if (role != OrganizationRole.Administrator &&
            (role != OrganizationRole.CaseWorker || assignedUserId != user.UserId))
        {
            throw new CaseAccessDeniedException();
        }
    }

    public static void RequireAssignment(OrganizationRole role)
    {
        if (role != OrganizationRole.Administrator)
        {
            throw new CaseAccessDeniedException();
        }
    }
}
