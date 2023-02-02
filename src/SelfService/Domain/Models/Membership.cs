namespace SelfService.Domain.Models;

public class Membership
{
    public string CapabilityId { get; set; }
    public Capability Capability { get; set; }

    public string UPN { get; set; }
    public Member Member { get; set; }
}