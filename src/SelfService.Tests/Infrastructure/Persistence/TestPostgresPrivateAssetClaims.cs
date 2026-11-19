using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.Infrastructure.Persistence;

public class TestPostgresPrivateAssetClaims
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task competing_owners_cannot_both_claim_an_unowned_asset()
    {
        await using var setupFactory = new ExternalDatabaseFactory();
        var setup = await setupFactory.CreateDbContext();
        var asset = new PrivateAsset(
            PrivateAssetId.New(), "key", "image.png", "image/png", 4, "editor", DateTime.UtcNow,
            null, PrivateAssetOwnerType.News, true
        );
        setup.PrivateAssets.Add(asset);
        await setup.SaveChangesAsync();
        var firstOwner = Guid.NewGuid();
        var secondOwner = Guid.NewGuid();

        try
        {
            async Task<bool> Claim(Guid owner)
            {
                await using var factory = new ExternalDatabaseFactory();
                var db = await factory.CreateDbContext();
                await using var transaction = await new RealDbTransactionFacade(db).BeginTransaction();
                try
                {
                    await new PrivateAssetRepository(db).Claim([(Guid)asset.Id], PrivateAssetOwnerType.News, owner, "editor");
                    await transaction.Commit();
                    return true;
                }
                catch (ArgumentException)
                {
                    await transaction.Rollback();
                    return false;
                }
            }

            var results = await Task.WhenAll(Claim(firstOwner), Claim(secondOwner));
            Assert.Single(results.Where(result => result));
            var saved = await setup.PrivateAssets.AsNoTracking().SingleAsync(current => current.Id == asset.Id);
            Assert.Contains(saved.OwnerId, new Guid?[] { firstOwner, secondOwner });
        }
        finally
        {
            await setup.PrivateAssets.Where(current => current.Id == asset.Id).ExecuteDeleteAsync();
        }
    }
}