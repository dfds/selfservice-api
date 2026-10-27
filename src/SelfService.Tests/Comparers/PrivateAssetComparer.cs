using SelfService.Domain.Models;

namespace SelfService.Tests.Comparers;

public class PrivateAssetComparer : IEqualityComparer<PrivateAsset?>
{
    public bool Equals(PrivateAsset? x, PrivateAsset? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (x is null || y is null)
            return false;

        return x.Id == y.Id
            && x.StorageKey == y.StorageKey
            && x.FileName == y.FileName
            && x.MimeType == y.MimeType
            && x.SizeInBytes == y.SizeInBytes
            && x.CreatedBy == y.CreatedBy
            && x.CreatedAt.Equals(y.CreatedAt)
            && x.OwnerId == y.OwnerId
            && x.OwnerType == y.OwnerType
            && x.IsInline == y.IsInline;
    }

    public int GetHashCode(PrivateAsset obj) =>
        HashCode.Combine(
            HashCode.Combine(obj.Id, obj.StorageKey, obj.FileName, obj.MimeType),
            HashCode.Combine(obj.SizeInBytes, obj.CreatedBy, obj.CreatedAt, obj.OwnerId),
            HashCode.Combine(obj.OwnerType, obj.IsInline)
        );
}
