using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class MembershipApprovalListApiResource
{
    public MembershipApprovalApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public MembershipApprovalListLinks Links { get; set; }

    public class MembershipApprovalListLinks
    {
        public ResourceLink Self { get; set; }

        public MembershipApprovalListLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public MembershipApprovalListApiResource(MembershipApprovalApiResource[] items, MembershipApprovalListLinks links)
    {
        Items = items;
        Links = links;
    }
}
