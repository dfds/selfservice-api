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

public class UserPreviewResponse
{
    public List<UserPreviewItem> Previews { get; set; } = new();
}

public class UserPreviewItem
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
    public string Subject { get; set; } = "";
    public string Html { get; set; } = "";
}
