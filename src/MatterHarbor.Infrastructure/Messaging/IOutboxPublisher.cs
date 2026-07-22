using MatterHarbor.Infrastructure.Persistence;

namespace MatterHarbor.Infrastructure.Messaging;

public interface IOutboxPublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken);
}
