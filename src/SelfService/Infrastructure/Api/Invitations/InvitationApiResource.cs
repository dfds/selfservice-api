using System.Text.Json.Serialization;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Invitations;

public class InvitationApiResource
{
    public string Id { get; set; }
    public string Invitee { get; set; }
    public string Description { get; set; }
    public string CreatedBy { get; set; }
    public string CreatedAt { get; set; }

    [JsonPropertyName("_links")]
    public InvitationLinks Links { get; set; }

    public class InvitationLinks
    {
        public ResourceLink Self { get; set; }
        public ResourceLink Accept { get; set; }
        public ResourceLink Decline { get; set; }

        public InvitationLinks(ResourceLink self, ResourceLink accept, ResourceLink decline)
        {
            Self = self;
            Accept = accept;
            Decline = decline;
        }
    }

    public InvitationApiResource(
        string id,
        string invitee,
        string description,
        string createdBy,
        string createdAt,
        InvitationLinks links
    )
    {
        Id = id;
        Invitee = invitee;
        Description = description;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        Links = links;
    }
}
