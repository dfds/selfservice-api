namespace SelfService.Domain.Models;

public abstract class AggregateRoot<TId> : Entity<TId>, IEventSource
{
    private readonly LinkedList<IDomainEvent> _domainEvents = new();

    protected AggregateRoot()
    {

    }

    protected AggregateRoot(TId id) : base(id)
    {

    }

    protected void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.AddLast(domainEvent);
    }

    public string GetEventSourceId()
    {
        return Id!.ToString()!;
    }

    public IEnumerable<IDomainEvent> GetEvents()
    {
        return _domainEvents;
    }

    public void ClearEvents()
    {
        _domainEvents.Clear();
    }
}