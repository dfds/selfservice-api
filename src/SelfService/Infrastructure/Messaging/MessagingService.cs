using Dafda.Outbox;
using Dafda.Producing;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Messaging;

public class MessagingService
{
    private readonly Producer _producer;

    public MessagingService(Producer producer)
    {
        _producer = producer;
    }

    public async Task SendDomainEvent(IDomainEvent evt)
    {
        await _producer.Produce(evt);
    }
}
