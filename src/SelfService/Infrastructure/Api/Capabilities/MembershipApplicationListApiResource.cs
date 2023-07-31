using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class MembershipApplicationListApiResource
{
    public MembershipApplicationApiResource[] Items { get; set; }

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

    public MembershipApplicationListApiResource(MembershipApplicationApiResource[] items, MembershipApplicationListLinks links)
    {
        Items = items;
        Links = links;
    }
}
