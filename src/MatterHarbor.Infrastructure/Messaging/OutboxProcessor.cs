using System.Diagnostics;
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
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public async Task<int> ProcessBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var candidates = await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(x => x.Status == OutboxStatus.Pending ||
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
                            (x.Status == OutboxStatus.Pending ||
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
                .SingleAsync(x => x.Id == messageId && x.LockId == lockId, cancellationToken);

            using var activity = ActivitySource.StartActivity("outbox.process");
            activity?.SetTag("messaging.message.id", message.Id);
            activity?.SetTag("messaging.message.type", message.Type);

            try
            {
                await publisher.PublishAsync(message, cancellationToken);
                message.Status = OutboxStatus.Processed;
                message.ProcessedAt = clock.UtcNow;
                message.LockId = null;
                message.LockedUntil = null;
                message.LastErrorCode = null;
                await dbContext.SaveChangesAsync(cancellationToken);
                processed++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                message.Status = OutboxStatus.Pending;
                message.LockId = null;
                message.LockedUntil = null;
                message.LastErrorCode = exception.GetType().Name;
                await dbContext.SaveChangesAsync(cancellationToken);
                LogPublishFailure(logger, message.Id, message.LastErrorCode);
            }
        }

        return processed;
    }

    [LoggerMessage(1002, LogLevel.Warning, "Outbox message {MessageId} failed with {ErrorCode}")]
    private static partial void LogPublishFailure(ILogger logger, Guid messageId, string? errorCode);
}
