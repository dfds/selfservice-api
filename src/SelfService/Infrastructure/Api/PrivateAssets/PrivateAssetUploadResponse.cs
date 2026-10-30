using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.PrivateAssets;

public class PrivateAssetUploadResponse
{
    public string AssetId { get; set; } = "";
    public Guid OwnerId { get; set; }
    public PrivateAssetOwnerType OwnerType { get; set; }
    public bool IsInline { get; set; }
    public long SizeInBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string StorageKey { get; set; } = "";
    public string FileName { get; set; } = "";
    public string MimeType { get; set; } = "application/octet-stream";
    public string? Url { get; set; }
}
