using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class MemberRemovedDueToMissingStatus : IDomainEvent
{
    public string UserId { get; set; }
    public string Status { get; set; } // NotFound or Deactivated

    public MemberRemovedDueToMissingStatus(string userId, string status)
    {
        UserId = userId;
        Status = status;
    }
}
