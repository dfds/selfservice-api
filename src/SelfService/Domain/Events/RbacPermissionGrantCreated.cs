using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class RbacPermissionGrantCreated : IDomainEvent
{
    public const string EventType = "rbac-permission-grant-created";
    
}