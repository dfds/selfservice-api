namespace SelfService.Infrastructure.Api.EmailCampaigns;

public class ResolveAudienceRequest
{
    public string? AudienceJson { get; set; }
    public string? RecipientFilter { get; set; }
}
