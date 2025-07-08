using System.Text.Json.Serialization;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.ReleaseNotes;

public class ReleaseNoteListApiResource
{
    public ReleaseNoteApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public ReleaseNoteListLinks Links { get; set; }

    public class ReleaseNoteListLinks
    {
        public ResourceLink Self { get; set; }

        public ResourceLink? CreateReleaseNote { get; set; }

        public ReleaseNoteListLinks(ResourceLink self, ResourceLink? createReleaseNote = null)
        {
            Self = self;
            CreateReleaseNote = createReleaseNote;
        }
    }

    public ReleaseNoteListApiResource(ReleaseNoteApiResource[] items, ReleaseNoteListLinks links)
    {
        Items = items;
        Links = links;
    }
}
