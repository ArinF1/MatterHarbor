namespace MatterHarbor.Domain.Organizations;

public sealed class Organization
{
    private Organization()
    {
    }

    public Organization(Guid id, string name)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Organization id and name are required.");
        }

        Id = id;
        Name = name.Trim();
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
