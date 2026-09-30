using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MatterHarbor.Application.Abstractions;
using MatterHarbor.Infrastructure.Persistence;

namespace MatterHarbor.Infrastructure.Messaging;

public sealed partial class OutboxProcessor(
    MatterHarborDbContext dbContext,
    IOutboxPublisher publisher,
    IClock clock,
    ILogger<OutboxProcessor> logger)
{
    public const string ActivitySourceName = "MatterHarbor.Worker";
    public const string MeterName = "MatterHarbor.Worker.Outbox";
    public const int PurgeBatchSize = 1000;
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Published = Meter.CreateCounter<long>("outbox.published");
    private static readonly Counter<long> Retried = Meter.CreateCounter<long>("outbox.retried");
    private static readonly Counter<long> DeadLettered = Meter.CreateCounter<long>("outbox.dead_lettered");
    private static readonly Counter<long> Purged = Meter.CreateCounter<long>("outbox.purged");
    private const int MaxAttempts = 10;

    public async Task<int> ProcessBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var candidates = await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(x => (x.Status == OutboxStatus.Pending &&
                         (x.NextAttemptAt == null || x.NextAttemptAt <= now)) ||
                        (x.Status == OutboxStatus.Processing && x.LockedUntil < now))
            .OrderBy(x => x.OccurredAt)
            .Select(x => x.Id)
            .Take(Math.Clamp(batchSize, 1, 100))
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var messageId in candidates)
        {
            var lockId = Guid.NewGuid();
            var claimed = await dbContext.OutboxMessages
                .Where(x => x.Id == messageId &&
                            ((x.Status == OutboxStatus.Pending &&
                              (x.NextAttemptAt == null || x.NextAttemptAt <= now)) ||
                             (x.Status == OutboxStatus.Processing && x.LockedUntil < now)))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, OutboxStatus.Processing)
                    .SetProperty(x => x.LockId, lockId)
                    .SetProperty(x => x.LockedUntil, now.AddMinutes(2))
                    .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1),
                    cancellationToken);

            if (claimed != 1)
            {
                continue;
            }

            var message = await dbContext.OutboxMessages
                .AsNoTracking()
                .SingleAsync(x => x.Id == messageId && x.LockId == lockId, cancellationToken);

            using var activity = ActivitySource.StartActivity("outbox.process");
            activity?.SetTag("messaging.message.id", message.Id);
            activity?.SetTag("messaging.message.type", message.Type);

            try
            {
                await publisher.PublishAsync(message, cancellationToken);
                var completed = await dbContext.OutboxMessages
                    .Where(x => x.Id == messageId && x.LockId == lockId && x.Status == OutboxStatus.Processing)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Status, OutboxStatus.Processed)
                        .SetProperty(x => x.ProcessedAt, clock.UtcNow)
                        .SetProperty(x => x.LockId, (Guid?)null)
                        .SetProperty(x => x.LockedUntil, (DateTimeOffset?)null)
                        .SetProperty(x => x.LastErrorCode, (string?)null)
                        .SetProperty(x => x.NextAttemptAt, (DateTimeOffset?)null),
                        cancellationToken);
                if (completed == 1)
                {
                    Published.Add(1);
                    processed++;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                var failedAt = clock.UtcNow;
                var exhausted = message.AttemptCount >= MaxAttempts;
                var status = exhausted ? OutboxStatus.DeadLetter : OutboxStatus.Pending;
                var errorCode = exception.GetType().Name;
                DateTimeOffset? nextAttemptAt = exhausted
                    ? null
                    : failedAt.AddSeconds(Math.Min(5 * (1 << Math.Min(message.AttemptCount - 1, 10)), 3600));
                DateTimeOffset? deadLetteredAt = exhausted ? failedAt : null;
                var failed = await dbContext.OutboxMessages
                    .Where(x => x.Id == messageId && x.LockId == lockId && x.Status == OutboxStatus.Processing)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Status, status)
                        .SetProperty(x => x.LockId, (Guid?)null)
                        .SetProperty(x => x.LockedUntil, (DateTimeOffset?)null)
                        .SetProperty(x => x.LastErrorCode, errorCode)
                        .SetProperty(x => x.NextAttemptAt, nextAttemptAt)
                        .SetProperty(x => x.DeadLetteredAt, deadLetteredAt),
                        cancellationToken);
                if (failed == 0)
                {
                    continue;
                }
                if (exhausted)
                {
                    DeadLettered.Add(1);
                }
                else
                {
                    Retried.Add(1);
                }
                LogPublishFailure(logger, message.Id, errorCode);
            }
        }

        return processed;
    }

    public async Task<int> PurgeProcessedAsync(CancellationToken cancellationToken)
    {
        var cutoff = clock.UtcNow.AddDays(-30);
        var ids = dbContext.OutboxMessages
            .Where(x => x.Status == OutboxStatus.Processed && x.ProcessedAt < cutoff)
            .OrderBy(x => x.ProcessedAt)
            .Select(x => x.Id)
            .Take(PurgeBatchSize);
        var count = await dbContext.OutboxMessages
            .Where(x => ids.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken);
        Purged.Add(count);
        return count;
    }

    [LoggerMessage(1002, LogLevel.Warning, "Outbox message {MessageId} failed with {ErrorCode}")]
    private static partial void LogPublishFailure(ILogger logger, Guid messageId, string? errorCode);
}
