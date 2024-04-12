using Dafda.Outbox;
using Dafda.Producing;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Messaging;

public interface IMessagingService
{
    public Task SendDomainEvent(IDomainEvent evt);
}

public class MessagingService : IMessagingService
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
