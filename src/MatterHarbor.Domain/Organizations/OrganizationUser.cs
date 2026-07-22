namespace MatterHarbor.Domain.Organizations;

public sealed class OrganizationUser
{
    private OrganizationUser()
    {
    }

    public OrganizationUser(Guid id, Guid organizationId, string externalSubject, string displayName)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty || string.IsNullOrWhiteSpace(externalSubject))
        {
            throw new ArgumentException("User id, organization id, and subject are required.");
        }

        Id = id;
        OrganizationId = organizationId;
        ExternalSubject = externalSubject.Trim();
        DisplayName = displayName.Trim();
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string ExternalSubject { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;
}
