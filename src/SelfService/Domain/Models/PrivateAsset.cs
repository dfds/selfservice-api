namespace SelfService.Domain.Models;

public class PrivateAsset : Entity<PrivateAssetId>
{
    public PrivateAsset(
        PrivateAssetId id,
        string storageKey,
        string fileName,
        string mimeType,
        long sizeInBytes,
        string createdBy,
        DateTime createdAt,
        Guid ownerId,
        PrivateAssetOwnerType ownerType,
        string contentId,
        bool isInline
    )
        : base(id)
    {
        StorageKey = ValidateRequired(storageKey, nameof(storageKey));
        if (StorageKey.Length > 1024)
            throw new ArgumentException("Asset metadata exceeds the supported field length.");
        ValidateMetadata(fileName, mimeType, sizeInBytes, createdBy, ownerId, ownerType, contentId);
        FileName = fileName;
        MimeType = mimeType;
        SizeInBytes = sizeInBytes;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        OwnerId = ownerId;
        OwnerType = ownerType;
        ContentId = contentId;
        IsInline = isInline;
    }

    public static void ValidateMetadata(
        string fileName,
        string mimeType,
        long sizeInBytes,
        string createdBy,
        Guid ownerId,
        PrivateAssetOwnerType ownerType,
        string contentId
    )
    {
        ValidateRequired(fileName, nameof(fileName));
        ValidateRequired(mimeType, nameof(mimeType));
        ValidateRequired(createdBy, nameof(createdBy));
        ValidateRequired(contentId, nameof(contentId));
        if (sizeInBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes), "Size must be zero or greater.");
        if (ownerId == Guid.Empty)
            throw new ArgumentException("Owner ID is required.", nameof(ownerId));
        if (!Enum.IsDefined(ownerType))
            throw new ArgumentException("Unsupported owner type.", nameof(ownerType));
        if (
            contentId.Length > 255
            || contentId.Any(character => char.IsWhiteSpace(character) || character is '<' or '>' or '"' or '\'')
        )
            throw new ArgumentException(
                "ContentId must be a MIME identifier of at most 255 characters.",
                nameof(contentId)
            );
        if (fileName.Length > 255 || mimeType.Length > 255 || createdBy.Length > 255)
            throw new ArgumentException("Asset metadata exceeds the supported field length.");
    }

    public string StorageKey { get; private set; }
    public string FileName { get; private set; }
    public string MimeType { get; private set; }
    public long SizeInBytes { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid OwnerId { get; private set; }
    public PrivateAssetOwnerType OwnerType { get; private set; }
    public string ContentId { get; private set; }
    public bool IsInline { get; private set; }

    private static string ValidateRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} cannot be empty.", parameterName);
        }

        return value;
    }
}
