using System.Text.Json.Serialization;

namespace SelfService.Domain.Models;

public class UserSettings
{
    [JsonPropertyName("signedUpForDemos")]
    public bool SignedUpForDemos { get; set; } = false;

    public static UserSettings Default => new();
}
