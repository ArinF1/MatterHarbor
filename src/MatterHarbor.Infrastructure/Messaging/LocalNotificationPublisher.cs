using Microsoft.Extensions.Logging;
using MatterHarbor.Infrastructure.Persistence;

namespace MatterHarbor.Infrastructure.Messaging;

public sealed partial class LocalNotificationPublisher(ILogger<LocalNotificationPublisher> logger) : IOutboxPublisher
{
    public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        LogNotification(logger, message.Id, message.Type);
        return Task.CompletedTask;
    }

    [LoggerMessage(1001, LogLevel.Information, "Handled local notification {MessageId} of type {MessageType}")]
    private static partial void LogNotification(ILogger logger, Guid messageId, string messageType);
}
