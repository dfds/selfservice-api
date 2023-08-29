using System.Text.Json.Serialization;

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

    [JsonPropertyName("identities")]
    public List<Identity>? Identities { get; set; }

    public User(
        string odataContext,
        string displayName,
        bool accountEnabled,
        string id,
        string mail,
        List<Identity> identities
    )
    {
        OdataContext = odataContext;
        DisplayName = displayName;
        AccountEnabled = accountEnabled;
        Id = id;
        Mail = mail;
        Identities = identities;
    }
}

public class Identity
{
    [JsonPropertyName("signInType")]
    public string? SignInType { get; set; }

    [JsonPropertyName("issuer")]
    public string? Issuer { get; set; }

    [JsonPropertyName("issuerAssignedId")]
    public string? IssuerAssignedId { get; set; }

    public Identity(string signInType, string issuer, string issuerAssignedId)
    {
        SignInType = signInType;
        Issuer = issuer;
        IssuerAssignedId = issuerAssignedId;
    }
}
