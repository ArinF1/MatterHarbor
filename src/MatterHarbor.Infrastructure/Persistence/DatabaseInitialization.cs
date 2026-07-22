using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MatterHarbor.Domain.Organizations;

namespace MatterHarbor.Infrastructure.Persistence;

public static class DatabaseInitialization
{
    public static readonly Guid NorthwindOrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ContosoOrganizationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid AlexUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid CaseyUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static async Task InitializeMatterHarborDatabaseAsync(
        this IServiceProvider services,
        bool seedDevelopmentData,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MatterHarborDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        if (!seedDevelopmentData)
        {
            return;
        }

        if (!await dbContext.Organizations.AnyAsync(x => x.Id == NorthwindOrganizationId, cancellationToken))
        {
            dbContext.Organizations.Add(new Organization(NorthwindOrganizationId, "Northwind Municipality"));
        }

        if (!await dbContext.Organizations.AnyAsync(x => x.Id == ContosoOrganizationId, cancellationToken))
        {
            dbContext.Organizations.Add(new Organization(ContosoOrganizationId, "Contoso Housing"));
        }

        if (!await dbContext.OrganizationUsers.AnyAsync(x => x.Id == AlexUserId, cancellationToken))
        {
            dbContext.OrganizationUsers.Add(
                new OrganizationUser(AlexUserId, NorthwindOrganizationId, "dev-alex", "Alex Morgan"));
        }

        if (!await dbContext.OrganizationUsers.AnyAsync(x => x.Id == CaseyUserId, cancellationToken))
        {
            dbContext.OrganizationUsers.Add(
                new OrganizationUser(CaseyUserId, ContosoOrganizationId, "dev-casey", "Casey Lee"));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
