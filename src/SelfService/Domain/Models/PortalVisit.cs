using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class PortalVisit : AggregateRoot<Guid>
{
    public PortalVisit(Guid id, UserId visitedBy, DateTime visitedAt)
        : base(id)
    {
        VisitedBy = visitedBy;
        VisitedAt = visitedAt;
    }

    public UserId VisitedBy { get; private set; }
    public DateTime VisitedAt { get; private set; }

    public static PortalVisit Register(UserId visitedBy, DateTime visitedAt)
    {
        var instance = new PortalVisit(Guid.NewGuid(), visitedBy, visitedAt);

        return instance;
    }
}
