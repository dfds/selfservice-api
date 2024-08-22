using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class MembershipApplicationThatUserCanApproveListApiResource
{
    public MembershipApplicationThatUserCanApproveApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public MembershipApplicationListLinks Links { get; set; }

    public class MembershipApplicationListLinks
    {
        public ResourceLink Self { get; set; }

        public MembershipApplicationListLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public MembershipApplicationThatUserCanApproveListApiResource(
        MembershipApplicationThatUserCanApproveApiResource[] items,
        MembershipApplicationListLinks links
    )
    {
        Items = items;
        Links = links;
    }
}
