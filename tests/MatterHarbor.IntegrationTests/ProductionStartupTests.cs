using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MatterHarbor.IntegrationTests;

public sealed class ProductionStartupTests
{
    [Fact]
    public async Task Production_startup_does_not_connect_to_or_migrate_the_database()
    {
        await using var factory = new ProductionApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class ProductionApiWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting(
                "ConnectionStrings:MatterHarbor",
                "Host=127.0.0.1;Port=1;Database=must-not-connect;Username=unused;Password=unused");
            builder.UseSetting("Authentication:Mode", "Oidc");
            builder.UseSetting("Authentication:Oidc:Authority", "https://identity.example.invalid");
            builder.UseSetting("Authentication:Oidc:Audience", "matterharbor-tests");
            builder.UseSetting("Messaging:Transport", "Local");
        }
    }
}
