namespace SelfService.Infrastructure.Api.EmailCampaigns;

public class ResolveAudienceResponse
{
    public int TotalCapabilities { get; set; }
    public int TotalRecipients { get; set; }
    public List<AudienceCapabilityItem> Capabilities { get; set; } = new();
}

public class AudienceCapabilityItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int MemberCount { get; set; }
    public List<RecipientItem> Recipients { get; set; } = new();
}

public class RecipientItem
{
    public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
}

public class ResolveUserAudienceResponse
{
    public int TotalRecipients { get; set; }
    public List<AudienceUserItem> Users { get; set; } = new();
    public List<string> UnmatchedEmails { get; set; } = new();
}

public class AudienceUserItem
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
}
