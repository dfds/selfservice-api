using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class NewMembershipApplicationHasBeenSubmitted : IDomainEvent
{
    public string? MembershipApplicationId { get; set; }
    public string? TargetId { get; set; }
    public string? TargetType { get; set; }
    public List<string>? Approvers { get; set; }
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
