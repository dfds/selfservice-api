namespace SelfService.Domain.Models;

public class Membership
{
    public string UPN { get; set; }
    public Member Member { get; set; }
    public string CapabilityId { get; set; }
    public Capability Capability { get; set; }

    public DateTime CreatedAt { get; set; }
}