using System.Text.Json.Serialization;

public class User
{
    [JsonPropertyName("@odata.context")]
    public string OdataContext { get; set; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; }

    [JsonPropertyName("accountEnabled")]
    public bool AccountEnabled { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("mail")]
    public string Mail { get; set; }

    [JsonPropertyName("identities")]
    public List<Identity> Identities { get; set; }
}

public class Identity
{
    [JsonPropertyName("signInType")]
    public string SignInType { get; set; }

    [JsonPropertyName("issuer")]
    public string Issuer { get; set; }

    [JsonPropertyName("issuerAssignedId")]
    public string IssuerAssignedId { get; set; }
}
