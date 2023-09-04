using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.BackgroundJobs;

public class Identity
{
    [JsonPropertyName("signInType")]
    public string? SignInType { get; set; }

    [JsonPropertyName("issuer")]
    public string? Issuer { get; set; }

    [JsonPropertyName("issuerAssignedId")]
    public string? IssuerAssignedId { get; set; }
}
