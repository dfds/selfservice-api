using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class MembershipApplicationApiResource
{
    public string Id { get; set; }
    public string Applicant { get; set; }
    public string SubmittedAt { get; set; }
    public string ExpiresOn { get; set; }
    public MembershipApprovalListApiResource Approvals { get; set; }

    [JsonPropertyName("_links")]
    public MembershipApplicationLinks Links { get; set; } = new();

    public class MembershipApplicationLinks
    {
        public ResourceLink Self { get; set; } = new();
    }
}
