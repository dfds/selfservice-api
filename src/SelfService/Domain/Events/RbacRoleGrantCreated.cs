using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class RbacRoleGrantCreated : IDomainEvent
{
    public const string EventType = "rbac-role-grant-created";
    public RbacRoleGrantId RbacRoleGrantId { get; init; } = null!;
}
