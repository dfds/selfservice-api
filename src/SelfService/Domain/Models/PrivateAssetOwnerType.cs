using System.Text.Json.Serialization;

namespace SelfService.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter<PrivateAssetOwnerType>))]
public enum PrivateAssetOwnerType
{
    EmailCampaign,
}
