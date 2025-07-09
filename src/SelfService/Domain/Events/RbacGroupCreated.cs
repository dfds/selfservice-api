using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class RbacGroupCreated : IDomainEvent
{
    public const string EventType = "rbac-group-created";
    
}