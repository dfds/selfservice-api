namespace SelfService.Infrastructure.Api.EmailCampaigns;

public class PreviewResponse
{
    public List<PreviewItem> Previews { get; set; } = new();
}

public class PreviewItem
{
    public string CapabilityId { get; set; } = "";
    public string CapabilityName { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Html { get; set; } = "";
}
