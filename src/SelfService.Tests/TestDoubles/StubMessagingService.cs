using SelfService.Domain.Models;
using SelfService.Infrastructure.Messaging;

namespace SelfService.Tests.TestDoubles;

public class StubMessagingService : IMessagingService
{
    public Task SendDomainEvent(IDomainEvent evt)
    {
        return Task.FromResult(0);
    }
}
