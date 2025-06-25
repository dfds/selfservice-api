using System.Text.Json.Serialization;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.ReleaseNotes;

public class ReleaseNoteApiResource
{
    public string Id { get; set; }
    public string Title { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; }
    public bool IsActive { get; set; }

    [JsonPropertyName("_links")]
    public ReleaseNoteLinks Links { get; set; }

    public class ReleaseNoteLinks
    {
        public ResourceLink Self { get; set; }
        public ResourceLink? ToggleIsActive { get; set; } 
    
        public ReleaseNoteLinks(ResourceLink self, ResourceLink? toggleIsActive = null)
        {
            Self = self;
            ToggleIsActive = toggleIsActive;
        }
    }

    public ReleaseNoteApiResource(
        string id,
        string title,
        DateTime releaseDate,
        string content,
        DateTime createdAt,
        string createdBy,
        DateTime modifiedAt,
        string modifiedBy,
        bool isActive,
        ReleaseNoteLinks links
    )
    {
        Id = id;
        Title = title;
        ReleaseDate = releaseDate;
        Content = content;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;
        IsActive = isActive;
        Links = links;
    }
}
