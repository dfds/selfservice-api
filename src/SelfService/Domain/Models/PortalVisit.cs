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

        // NOTE [jandr@2023-04-21]: eeh, we don't need this right now

        //instance.Raise(new NewPortalVisitRegistered
        //{
        //    PortalVisitId = instance.Id.ToString("N"),
        //    VisitedBy = instance.VisitedBy,
        //    VisitedAt = instance.VisitedAt.ToUniversalTime().ToString("O"),
        //});

        return instance;
    }
}
