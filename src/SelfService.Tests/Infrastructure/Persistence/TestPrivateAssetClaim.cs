using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Events;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.Infrastructure.Persistence;

public class TestPrivateAssetClaim
{
    private static PrivateAsset Asset(string creator, PrivateAssetOwnerType type = PrivateAssetOwnerType.News) =>
        new(PrivateAssetId.New(), "key", "image.png", "image/png", 4, creator, DateTime.UtcNow, null, type, true);

    [Fact]
    public async Task upload_without_owner_persists_and_empty_owner_is_rejected()
    {
        await using var factory = new InMemoryDatabaseFactory();
        await using var db = await factory.CreateSelfServiceDbContext();
        var storage = new Mock<IPrivateAssetService>();
        storage
            .Setup(service =>
                service.UploadAsync("image.png", "image/png", It.IsAny<Stream>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync("key");
        var authorization = new Mock<IAuthorizationService>();
        authorization.Setup(service => service.CanCreatePrivateAsset(It.IsAny<PortalUser>())).ReturnsAsync(true);
        var service = new PrivateAssetApplicationService(
            new PrivateAssetRepository(db),
            storage.Object,
            authorization.Object,
            new RealDbTransactionFacade(db),
            NullLogger<PrivateAssetApplicationService>.Instance
        );
        var user = new PortalUser(UserId.Parse("tester@dfds.com"), [UserRole.CloudEngineer]);

        var asset = await service.Upload(
            null,
            PrivateAssetOwnerType.News,
            true,
            "image.png",
            "image/png",
            4,
            new MemoryStream([1, 2, 3, 4]),
            user
        );
        db.ChangeTracker.Clear();
        Assert.Null((await db.PrivateAssets.SingleAsync()).OwnerId);
        Assert.Equal(user.Id.ToString(), asset.CreatedBy);
        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                service.Upload(
                    Guid.Empty,
                    PrivateAssetOwnerType.News,
                    true,
                    "image.png",
                    "image/png",
                    4,
                    new MemoryStream([1, 2, 3, 4]),
                    user
                )
        );
        storage.Verify(
            current =>
                current.UploadAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task uploader_can_claim_asset_and_other_editors_can_keep_it()
    {
        await using var factory = new InMemoryDatabaseFactory();
        await using var db = await factory.CreateSelfServiceDbContext();
        var asset = Asset("editor");
        db.PrivateAssets.Add(asset);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var owner = Guid.NewGuid();
        var repository = new PrivateAssetRepository(db);

        await repository.Claim([(Guid)asset.Id, (Guid)asset.Id], PrivateAssetOwnerType.News, owner, "editor");
        await repository.Claim([(Guid)asset.Id], PrivateAssetOwnerType.News, owner, "other");

        Assert.Equal(owner, (await db.PrivateAssets.SingleAsync()).OwnerId);
        await Assert.ThrowsAsync<ArgumentException>(
            () => repository.Claim([(Guid)asset.Id], PrivateAssetOwnerType.News, Guid.NewGuid(), "editor")
        );
    }

    [Fact]
    public async Task claim_rejects_wrong_uploader_type_and_missing_asset()
    {
        await using var factory = new InMemoryDatabaseFactory();
        await using var db = await factory.CreateSelfServiceDbContext();
        var asset = Asset("editor");
        db.PrivateAssets.Add(asset);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var owner = Guid.NewGuid();
        var repository = new PrivateAssetRepository(db);

        await Assert.ThrowsAsync<ArgumentException>(
            () => repository.Claim([(Guid)asset.Id], PrivateAssetOwnerType.News, owner, "other")
        );
        await Assert.ThrowsAsync<ArgumentException>(
            () => repository.Claim([(Guid)asset.Id], PrivateAssetOwnerType.Event, owner, "editor")
        );
        await Assert.ThrowsAsync<ArgumentException>(
            () => repository.Claim([Guid.NewGuid()], PrivateAssetOwnerType.News, owner, "editor")
        );
        Assert.Null((await db.PrivateAssets.SingleAsync()).OwnerId);
    }

    [Fact]
    public async Task event_save_failure_rolls_back_item_attachment_and_claim()
    {
        await using var factory = new InMemoryDatabaseFactory();
        await using var db = await factory.CreateSelfServiceDbContext();
        var asset = Asset("editor", PrivateAssetOwnerType.Event);
        var eventModel = new Event(
            new EventId(),
            DateTime.UtcNow,
            "Original",
            "Body",
            EventType.Demo,
            "editor",
            DateTime.UtcNow
        );
        var attachment = new EventAttachment(
            new EventAttachmentId(),
            eventModel.Id,
            "https://example.test",
            EventAttachmentType.Image,
            null,
            DateTime.UtcNow
        );
        db.PrivateAssets.Add(asset);
        db.Events.Add(eventModel);
        db.EventAttachments.Add(attachment);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        using var services = TransactionalServiceFactory.Create(db);
        var service = services.GetRequiredService<IEventService>();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () =>
                service.UpdateEvent(
                    eventModel.Id,
                    new EventUpdateRequest
                    {
                        Title = "Not saved",
                        AssetIds = [(Guid)asset.Id],
                        Attachments =
                        [
                            new EventAttachmentDto { Id = new EventAttachmentId(), Url = "https://missing.test" },
                        ],
                    },
                    "editor"
                )
        );

        db.ChangeTracker.Clear();
        Assert.Null((await db.PrivateAssets.SingleAsync()).OwnerId);
        Assert.Equal("Original", (await db.Events.SingleAsync()).Title);
        Assert.Equal(attachment.Id, (await db.EventAttachments.SingleAsync()).Id);
    }

    [Fact]
    public async Task failed_batch_rolls_back_prior_claims()
    {
        await using var factory = new InMemoryDatabaseFactory();
        await using var db = await factory.CreateSelfServiceDbContext();
        var firstId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var missingId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var asset = new PrivateAsset(
            PrivateAssetId.NewFrom(firstId),
            "key",
            "image.png",
            "image/png",
            4,
            "editor",
            DateTime.UtcNow,
            null,
            PrivateAssetOwnerType.News,
            true
        );
        db.PrivateAssets.Add(asset);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        using var services = TransactionalServiceFactory.Create(db);
        var service = services.GetRequiredService<INewsItemService>();
        var newsItem = new NewsItem(
            new NewsItemId(),
            "Title",
            "Body",
            DateTime.UtcNow,
            false,
            "editor",
            DateTime.UtcNow
        );

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateNewsItem(newsItem, [firstId, missingId], "editor")
        );

        Assert.Empty(await db.NewsItems.ToListAsync());
        Assert.Null((await db.PrivateAssets.AsNoTracking().SingleAsync()).OwnerId);
    }

    [Fact]
    public async Task save_failure_rolls_back_flushed_item_and_claim()
    {
        await using var factory = new InMemoryDatabaseFactory();
        await using var db = await factory.CreateSelfServiceDbContext();
        var asset = Asset("editor");
        db.PrivateAssets.Add(asset);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var newsItem = new NewsItem(
            new NewsItemId(),
            "title",
            "body",
            DateTime.UtcNow,
            false,
            "editor",
            DateTime.UtcNow
        );
        var repository = new Mock<INewsItemRepository>();
        repository
            .Setup(current => current.Add(It.IsAny<NewsItem>()))
            .Returns(
                async (NewsItem item) =>
                {
                    db.NewsItems.Add(item);
                    await db.SaveChangesAsync();
                    throw new InvalidOperationException();
                }
            );
        using var services = TransactionalServiceFactory.Create(db, repository.Object);
        var service = services.GetRequiredService<INewsItemService>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateNewsItem(newsItem, [(Guid)asset.Id], "editor")
        );
        db.ChangeTracker.Clear();
        Assert.Empty(await db.NewsItems.ToListAsync());
        Assert.Null((await db.PrivateAssets.SingleAsync()).OwnerId);
    }
}
