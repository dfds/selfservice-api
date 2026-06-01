namespace SelfService.Infrastructure.Api.EmailCampaigns;

public class ResolveAudienceRequest
{
    public string? AudienceJson { get; set; }
    public string? RecipientFilter { get; set; }

    /// <summary>"Capability" (default) or "User".</summary>
    public string? TargetType { get; set; }
}
