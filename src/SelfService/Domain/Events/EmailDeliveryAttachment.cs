namespace SelfService.Domain.Events;

public class EmailDeliveryAttachment
{
    public string ContentId { get; set; } = "";
    public string StorageKey { get; set; } = "";
    public string AssetId { get; set; } = "";
    public bool IsInline { get; set; }
}
