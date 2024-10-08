using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.BackgroundJobs;

public class User
{
    [JsonPropertyName("@odata.context")]
    public string? OdataContext { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("accountEnabled")]
    public bool AccountEnabled { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("mail")]
    public string? Mail { get; set; }

    [JsonPropertyName("userPrincipalName")]
    public string? UserPrincipalName { get; set; }

    [JsonPropertyName("identities")]
    public List<Identity>? Identities { get; set; }
}

public class UsersResponse
{
    [JsonPropertyName("@odata.context")]
    public string? OdataContext { get; set; }

    [JsonPropertyName("value")]
    public List<User>? Value { get; set; }
}
