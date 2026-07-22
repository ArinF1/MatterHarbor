using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using MatterHarbor.Infrastructure.Persistence;

namespace MatterHarbor.Api.Authentication;

public sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Development";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var persona = Request.Headers["X-MatterHarbor-User"].ToString();
        var identity = persona.ToLowerInvariant() switch
        {
            "alex" => CreateIdentity(
                DatabaseInitialization.AlexUserId,
                DatabaseInitialization.NorthwindOrganizationId,
                "Alex Morgan"),
            "casey" => CreateIdentity(
                DatabaseInitialization.CaseyUserId,
                DatabaseInitialization.ContosoOrganizationId,
                "Casey Lee"),
            _ => null
        };

        if (identity is null)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static ClaimsIdentity CreateIdentity(Guid userId, Guid organizationId, string name)
    {
        return new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, name),
            new Claim("org_id", organizationId.ToString())
        ], SchemeName);
    }
}
