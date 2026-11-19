using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Application;

public class PrivateAssetOwnerSave(IPrivateAssetRepository assets, IDbTransactionFacade transactions)
{
    public async Task<T> Execute<T>(
        IEnumerable<Guid>? assetIds,
        PrivateAssetOwnerType ownerType,
        Guid ownerId,
        string userId,
        Func<Task<T>> save
    )
    {
        await using var transaction = await transactions.BeginTransaction();
        try
        {
            await assets.Claim(assetIds ?? [], ownerType, ownerId, userId);
            var result = await save();
            await transaction.Commit();
            return result;
        }
        catch
        {
            await transaction.Rollback();
            throw;
        }
    }
}
