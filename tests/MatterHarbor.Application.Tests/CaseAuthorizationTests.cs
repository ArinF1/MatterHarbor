using MatterHarbor.Application.Cases;
using MatterHarbor.Domain.Organizations;

namespace MatterHarbor.Application.Tests;

public sealed class CaseAuthorizationTests
{
    [Fact]
    public void Unknown_role_cannot_create_a_case()
    {
        var user = new UserContext(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<CaseAccessDeniedException>(() =>
            CaseAuthorization.RequireCreate((OrganizationRole)999, user, null));
    }

    [Fact]
    public void Case_worker_can_assign_only_themselves_on_creation()
    {
        var user = new UserContext(Guid.NewGuid(), Guid.NewGuid());

        CaseAuthorization.RequireCreate(OrganizationRole.CaseWorker, user, user.UserId);
        Assert.Throws<CaseAccessDeniedException>(() =>
            CaseAuthorization.RequireCreate(OrganizationRole.CaseWorker, user, Guid.NewGuid()));
    }
}
