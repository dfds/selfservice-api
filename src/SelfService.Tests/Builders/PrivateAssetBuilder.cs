using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class PrivateAssetBuilder
{
    private PrivateAssetId _id = PrivateAssetId.New();
    private string _storageKey = "private-assets/test/asset.png";
    private string _fileName = "asset.png";
    private string _mimeType = "image/png";
    private long _sizeInBytes = 4;
    private string _createdBy = "tester@dfds.com";
    private DateTime _createdAt = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private Guid _ownerId = Guid.NewGuid();
    private PrivateAssetOwnerType _ownerType = PrivateAssetOwnerType.EmailCampaign;
    private string _contentId = "asset";
    private bool _isInline = true;

    public PrivateAssetBuilder WithId(PrivateAssetId id)
    {
        _id = id;
        return this;
    }

    public PrivateAssetBuilder WithStorageKey(string value)
    {
        _storageKey = value;
        return this;
    }

    public PrivateAssetBuilder WithFileName(string value)
    {
        _fileName = value;
        return this;
    }

    public PrivateAssetBuilder WithMimeType(string value)
    {
        _mimeType = value;
        return this;
    }

    public PrivateAssetBuilder WithSizeInBytes(long value)
    {
        _sizeInBytes = value;
        return this;
    }

    public PrivateAssetBuilder WithCreatedBy(string value)
    {
        _createdBy = value;
        return this;
    }

    public PrivateAssetBuilder WithCreatedAt(DateTime value)
    {
        _createdAt = value;
        return this;
    }

    public PrivateAssetBuilder WithOwnerId(Guid value)
    {
        _ownerId = value;
        return this;
    }

    public PrivateAssetBuilder WithOwnerType(PrivateAssetOwnerType value)
    {
        _ownerType = value;
        return this;
    }

    public PrivateAssetBuilder WithContentId(string value)
    {
        _contentId = value;
        return this;
    }

    public PrivateAssetBuilder WithIsInline(bool value)
    {
        _isInline = value;
        return this;
    }

    public PrivateAsset Build() =>
        new(
            _id,
            _storageKey,
            _fileName,
            _mimeType,
            _sizeInBytes,
            _createdBy,
            _createdAt,
            _ownerId,
            _ownerType,
            _contentId,
            _isInline
        );

    public static implicit operator PrivateAsset(PrivateAssetBuilder builder) => builder.Build();
}
