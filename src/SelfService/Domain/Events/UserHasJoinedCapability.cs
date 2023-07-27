using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class UserHasJoinedCapability : IDomainEvent
{
    public string? MembershipId { get; set; }
    public string? CapabilityId { get; set; }
    public string? UserId { get; set; }
}