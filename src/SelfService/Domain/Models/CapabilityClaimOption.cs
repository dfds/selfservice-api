namespace SelfService.Domain.Models;

public class CapabilityClaimOption
{
    public CapabilityClaimOption(string claimType, string claimDescription)
    {
        ClaimType = claimType;
        ClaimDescription = claimDescription;
    }

    public string ClaimType { get; private set; }
    public string ClaimDescription { get; private set; }
}
