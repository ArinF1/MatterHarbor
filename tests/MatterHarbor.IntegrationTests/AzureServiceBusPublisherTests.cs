using Azure.Messaging.ServiceBus;
using MatterHarbor.Infrastructure.Messaging;
using MatterHarbor.Infrastructure.Persistence;

namespace MatterHarbor.IntegrationTests;

public sealed class AzureServiceBusPublisherTests
{
    [Fact]
    public async Task Publisher_sends_stable_message_identity_and_json_contract()
    {
        var sender = new RecordingSender();
        var publisher = new AzureServiceBusOutboxPublisher(sender);
        var message = new OutboxMessage
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Type = "case.created",
            Payload = "{\"caseId\":\"33333333-3333-3333-3333-333333333333\"}"
        };

        await publisher.PublishAsync(message, CancellationToken.None);

        Assert.NotNull(sender.Sent);
        Assert.Equal(message.Id.ToString("N"), sender.Sent.MessageId);
        Assert.Equal(message.Type, sender.Sent.Subject);
        Assert.Equal("application/json", sender.Sent.ContentType);
        Assert.Equal(message.Payload, sender.Sent.Body.ToString());
    }

    private sealed class RecordingSender : ServiceBusSender
    {
        public ServiceBusMessage? Sent { get; private set; }

        public override Task SendMessageAsync(
            ServiceBusMessage message,
            CancellationToken cancellationToken = default)
        {
            Sent = message;
            return Task.CompletedTask;
        }
    }
}
