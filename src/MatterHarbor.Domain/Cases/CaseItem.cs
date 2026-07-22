namespace MatterHarbor.Domain.Cases;

public sealed class CaseItem
{
    private CaseItem()
    {
    }

    private CaseItem(
        Guid id,
        Guid organizationId,
        string caseNumber,
        string title,
        string description,
        CasePriority priority,
        Guid? assignedUserId,
        DateTimeOffset now)
    {
        Id = id;
        OrganizationId = organizationId;
        CaseNumber = caseNumber;
        Title = title;
        Description = description;
        Priority = priority;
        Status = CaseStatus.New;
        AssignedUserId = assignedUserId;
        CreatedAt = now;
        UpdatedAt = now;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string CaseNumber { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public CasePriority Priority { get; private set; }

    public CaseStatus Status { get; private set; }

    public Guid? AssignedUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public int Version { get; private set; }

    public static CaseItem Create(
        Guid organizationId,
        string caseNumber,
        string title,
        string description,
        CasePriority priority,
        Guid? assignedUserId,
        DateTimeOffset now,
        Guid? id = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DomainValidationException("An organization is required.");
        }

        ValidateText(title, nameof(title), 1, 200);
        ValidateText(description, nameof(description), 1, 4_000);
        ValidateText(caseNumber, nameof(caseNumber), 1, 40);

        if (!Enum.IsDefined(priority))
        {
            throw new DomainValidationException("Priority is invalid.");
        }

        return new CaseItem(
            id ?? Guid.NewGuid(),
            organizationId,
            caseNumber.Trim(),
            title.Trim(),
            description.Trim(),
            priority,
            assignedUserId,
            now);
    }

    public void ChangeStatus(CaseStatus status, int expectedVersion, DateTimeOffset now)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainValidationException("Status is invalid.");
        }

        if (expectedVersion != Version)
        {
            throw new ConcurrencyConflictException();
        }

        Status = status;
        UpdatedAt = now;
    }

    private static void ValidateText(string value, string name, int minimum, int maximum)
    {
        var length = value?.Trim().Length ?? 0;
        if (length < minimum || length > maximum)
        {
            throw new DomainValidationException($"{name} must contain between {minimum} and {maximum} characters.");
        }
    }
}
