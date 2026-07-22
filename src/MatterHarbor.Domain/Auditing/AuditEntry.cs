namespace MatterHarbor.Domain.Auditing;

public sealed class AuditEntry
{
    private AuditEntry()
    {
    }

    public AuditEntry(
        Guid id,
        Guid organizationId,
        Guid actorUserId,
        Guid entityId,
        string action,
        DateTimeOffset occurredAt)
    {
        Id = id;
        OrganizationId = organizationId;
        ActorUserId = actorUserId;
        EntityId = entityId;
        Action = action;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ActorUserId { get; private set; }

    public Guid EntityId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }
}
