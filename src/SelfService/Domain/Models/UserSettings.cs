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

    public static UserSettings Default => new();
}
