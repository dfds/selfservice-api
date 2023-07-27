using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IPortalVisitApplicationService
{
    Task RegisterVisit(UserId userId);
}