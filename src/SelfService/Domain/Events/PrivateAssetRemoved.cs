using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class PrivateAssetRemoved : IDomainEvent
{
    public const string EventType = "private-asset-removed";

    public string? AssetId { get; set; }
    public string? StorageKey { get; set; }
}
