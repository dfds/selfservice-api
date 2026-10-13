namespace SelfService.Domain.Models;

public enum ContentImageOwnerType
{
    NewsItem,
    Event,
    ReleaseNote,
    EmailCampaign,
}

public sealed class ContentImage
{
    private ContentImage() { }

    public ContentImage(
        Guid id,
        ContentImageOwnerType ownerType,
        Guid ownerId,
        string objectKey,
        string fileName,
        string contentType,
        long length,
        DateTime createdAt
    )
    {
        Id = id;
        OwnerType = ownerType;
        OwnerId = ownerId;
        ObjectKey = objectKey;
        FileName = fileName;
        ContentType = contentType;
        Length = length;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public ContentImageOwnerType OwnerType { get; private set; }
    public Guid OwnerId { get; private set; }
    public string ObjectKey { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long Length { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsDeletePending { get; private set; }

    public void MarkDeletePending() => IsDeletePending = true;
}