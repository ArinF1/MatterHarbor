using Azure.Messaging.ServiceBus;
using MatterHarbor.Infrastructure.Persistence;

namespace MatterHarbor.Infrastructure.Messaging;

public sealed class AzureServiceBusOutboxPublisher(ServiceBusSender sender) : IOutboxPublisher
{
    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var serviceBusMessage = new ServiceBusMessage(BinaryData.FromString(message.Payload))
        {
            MessageId = message.Id.ToString("N"),
            Subject = message.Type,
            ContentType = "application/json"
        };
        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }
}
