using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class RbacRoleCreated : IDomainEvent
{
    public const string EventType = "rbac-role-created";
}
