namespace SelfService.Domain.Models;

public interface IPortalVisitRepository
{
    Task Add(PortalVisit portalVisit);
}