namespace MatterHarbor.Infrastructure.Persistence;

public sealed class IdempotencyRecord
{
    public Guid OrganizationId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string RequestHash { get; set; } = string.Empty;

    public string ResponseJson { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}

public enum OutboxStatus
{
    Pending = 1,
    Processing = 2,
    Processed = 3
}

public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;

    public int AttemptCount { get; set; }

    public Guid? LockId { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public string? LastErrorCode { get; set; }
}
