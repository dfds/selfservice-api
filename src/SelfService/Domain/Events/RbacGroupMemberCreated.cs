using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class RbacGroupMemberCreated : IDomainEvent
{
    public const string EventType = "rbac-group-member-created";
    
}