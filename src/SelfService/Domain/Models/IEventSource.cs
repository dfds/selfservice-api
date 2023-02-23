namespace SelfService.Domain.Models;

public interface IEventSource
{
    string GetEventSourceId();
    IEnumerable<IDomainEvent> GetEvents();
    void ClearEvents();
}