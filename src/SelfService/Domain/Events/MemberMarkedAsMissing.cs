using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class MemberMarkedAsMissing : IDomainEvent
{
    public string UserId { get; set; }
    public string Status { get; set; } // NotFound or Deactivated

    public MemberMarkedAsMissing(string userId, string status)
    {
        UserId = userId;
        Status = status;
    }
}
