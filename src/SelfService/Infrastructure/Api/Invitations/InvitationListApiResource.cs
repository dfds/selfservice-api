using System.Text.Json.Serialization;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Invitations;

public class InvitationListApiResource
{
    public InvitationApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public InvitationListLinks Links { get; set; }

    public class InvitationListLinks
    {
        public ResourceLink Self { get; set; }

        public InvitationListLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public InvitationListApiResource(InvitationApiResource[] items, InvitationListLinks links)
    {
        Items = items;
        Links = links;
    }
}
