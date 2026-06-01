namespace SelfService.Infrastructure.Api.EmailCampaigns;

public class PreviewRequest
{
    public string[]? CapabilityIds { get; set; }

    /// <summary>Email addresses for previewing User-targeted campaigns.</summary>
    public string[]? UserEmails { get; set; }
}
