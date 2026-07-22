using System.Security.Claims;
using MatterHarbor.Application.Cases;

namespace MatterHarbor.Api.Authentication;

public static class CurrentUserExtensions
{
    public static UserContext GetMatterHarborUser(this ClaimsPrincipal principal)
    {
        var userValue = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        var organizationValue = principal.FindFirstValue("org_id");

        if (!Guid.TryParse(userValue, out var userId) ||
            !Guid.TryParse(organizationValue, out var organizationId))
        {
            throw new InvalidUserContextException();
        }

        return new UserContext(userId, organizationId);
    }
}

public sealed class InvalidUserContextException()
    : Exception("The authenticated identity does not contain valid MatterHarbor user and organization claims.");
