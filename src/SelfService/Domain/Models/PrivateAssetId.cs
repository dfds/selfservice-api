namespace SelfService.Domain.Models;

public class PrivateAssetId : ValueObjectGuid<PrivateAssetId>
{
    private PrivateAssetId(Guid newGuid)
        : base(newGuid) { }
}
