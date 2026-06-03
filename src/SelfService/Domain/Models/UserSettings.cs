using System.Text.Json.Serialization;

namespace SelfService.Domain.Models;

public class UserSettings
{
    [JsonPropertyName("signedUpForDemos")]
    public bool SignedUpForDemos { get; set; } = false;

    [JsonPropertyName("showOnlyMyCapabilities")]
    public bool ShowOnlyMyCapabilities { get; set; } = false;

    [JsonPropertyName("showDeletedCapabilities")]
    public bool ShowDeletedCapabilities { get; set; } = false;

    [JsonPropertyName("seenWhatsNewIds")]
    public List<string> SeenWhatsNewIds { get; set; } = new();

    [JsonPropertyName("dismissedWhatsNewIds")]
    public List<string> DismissedWhatsNewIds { get; set; } = new();

    [JsonPropertyName("completedWhatsNewIds")]
    public List<string> CompletedWhatsNewIds { get; set; } = new();

    public static UserSettings Default => new();
}
