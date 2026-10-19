using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.Builders;

namespace SelfService.Tests.Application;

public class TestPrivateAssetApplicationService
{
    private static readonly PortalUser Editor = new(UserId.Parse("tester@dfds.com"), [UserRole.CloudEngineer]);

    private static EmailCampaign Draft() =>
        EmailCampaign.CreateDraft(
            "Campaign",
            "Subject",
            "{}",
            "<p></p>",
            "{\"mode\":\"all\"}",
            null,
            Editor.Id.ToString()
        );

    private static PrivateAssetApplicationService Service(
        SelfServiceDbContext db,
        Mock<IPrivateAssetService> storage,
        IDbTransactionFacade? transactions = null,
        IAuthorizationService? authorization = null
    ) =>
        new(
            new PrivateAssetRepository(db),
            storage.Object,
            authorization
                ?? Mock.Of<IAuthorizationService>(authorization =>
                    authorization.CanCreatePrivateAsset(It.IsAny<PortalUser>())
                ),
            transactions ?? new RealDbTransactionFacade(db),
            NullLogger<PrivateAssetApplicationService>.Instance
        );

    [Fact]
    public async Task upload_commits_metadata_before_preview_and_signing_failure_preserves_it()
    {
        await using var factory = new InMemoryDatabaseFactory();
        await using var db = await factory.CreateSelfServiceDbContext();
        var owner = Draft();
        db.EmailCampaigns.Add(owner);
        await db.SaveChangesAsync();
        var storage = new Mock<IPrivateAssetService>();
        storage
            .Setup(service =>
                service.UploadAsync("banner.png", "image/png", It.IsAny<Stream>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync("private-assets/upload/banner.png");
        storage
            .Setup(service =>
                service.GetPrivateUrlAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new InvalidOperationException("Signing unavailable"));
        var service = Service(db, storage);

        var asset = await service.Upload(
            owner.Id,
            PrivateAssetOwnerType.EmailCampaign,
            "banner",
            true,
            "banner.png",
            "image/png",
            4,
            new MemoryStream([1, 2, 3, 4]),
            Editor
        );
        storage.Verify(
            service =>
                service.GetPrivateUrlAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        db.ChangeTracker.Clear();
        var persisted = await service.Get(asset.Id);
        Assert.Equal("private-assets/upload/banner.png", persisted.StorageKey);
        Assert.Equal(Editor.Id.ToString(), persisted.CreatedBy);
        Assert.Equal((Guid)owner.Id, persisted.OwnerId);
        Assert.Equal(PrivateAssetOwnerType.EmailCampaign, persisted.OwnerType);
        Assert.Equal("banner.png", persisted.FileName);
        Assert.Null(await service.GetPreviewUrl(persisted));
        Assert.Single(await service.GetByOwner(owner.Id, PrivateAssetOwnerType.EmailCampaign));
    }

    [Fact]
    public void asset_rejects_undefined_owner_enum_value()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new PrivateAsset(
                    PrivateAssetId.New(),
                    "private-assets/test/banner.png",
                    "banner.png",
                    "image/png",
                    4,
                    "tester",
                    DateTime.UtcNow,
                    Guid.NewGuid(),
                    (PrivateAssetOwnerType)(-1),
                    "banner",
                    true
                )
        );
    }

    [Fact]
    public async Task upload_does_not_require_an_owner_record()
    {
        await using var factory = new InMemoryDatabaseFactory();
        await using var db = await factory.CreateSelfServiceDbContext();
        var storage = new Mock<IPrivateAssetService>();
        storage
            .Setup(service =>
                service.UploadAsync("banner.png", "image/png", It.IsAny<Stream>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync("private-assets/upload/banner.png");

        var asset = await Service(db, storage)
            .Upload(
                Guid.NewGuid(),
                PrivateAssetOwnerType.EmailCampaign,
                "banner",
                true,
                "banner.png",
                "image/png",
                4,
                new MemoryStream([1, 2, 3, 4]),
                Editor
            );

        Assert.Equal(PrivateAssetOwnerType.EmailCampaign, asset.OwnerType);
        Assert.Single(await new PrivateAssetRepository(db).GetByOwner(asset.OwnerType, asset.OwnerId));
        Assert.Empty(db.EmailCampaigns);
    }

    [Fact]
    public async Task invalid_metadata_is_rejected_before_storage_upload()
    {
        await using var factory = new InMemoryDatabaseFactory();
        await using var db = await factory.CreateSelfServiceDbContext();
        var storage = new Mock<IPrivateAssetService>(MockBehavior.Strict);

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                Service(db, storage)
                    .Upload(
                        Guid.NewGuid(),
                        PrivateAssetOwnerType.EmailCampaign,
                        "bad content id",
                        true,
                        "banner.png",
                        "image/png",
                        4,
                        new MemoryStream([1, 2, 3, 4]),
                        Editor
                    )
        );

        storage.VerifyNoOtherCalls();
        Assert.Empty(db.PrivateAssets);
    }

    [Fact]
    public async Task commit_failure_never_returns_success_or_signs_a_preview()
    {
        await using var factory = new InMemoryDatabaseFactory();
        await using var db = await factory.CreateSelfServiceDbContext();
        var owner = Draft();
        db.EmailCampaigns.Add(owner);
        await db.SaveChangesAsync();
        var storage = new Mock<IPrivateAssetService>();
        storage
            .Setup(service =>
                service.UploadAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("private-assets/retained/banner.png");
        var transaction = new Mock<IDbTransaction>();
        transaction.Setup(current => current.Commit()).ThrowsAsync(new IOException("Commit failed"));
        var transactions = new Mock<IDbTransactionFacade>();
        transactions.Setup(current => current.BeginTransaction()).ReturnsAsync(transaction.Object);

        await Assert.ThrowsAsync<IOException>(
            () =>
                Service(db, storage, transactions.Object)
                    .Upload(
                        owner.Id,
                        PrivateAssetOwnerType.EmailCampaign,
                        "banner",
                        true,
                        "banner.png",
                        "image/png",
                        4,
                        new MemoryStream([1, 2, 3, 4]),
                        Editor
                    )
        );
        storage.Verify(
            service =>
                service.GetPrivateUrlAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        db.ChangeTracker.Clear();
        Assert.Empty(db.PrivateAssets);
    }

    [Fact]
    public async Task owned_metadata_is_committed_and_reloaded_by_owner()
    {
        await using var factory = new InMemoryDatabaseFactory();
        await using var db = await factory.CreateSelfServiceDbContext();
        var repository = new PrivateAssetRepository(db);
        var ownerId = Guid.NewGuid();
        var asset = A
            .PrivateAsset.WithStorageKey("private-assets/test/banner.png")
            .WithFileName("banner.png")
            .WithOwnerId(ownerId)
            .WithContentId("banner")
            .Build();

        await using var transaction = await new RealDbTransactionFacade(db).BeginTransaction();
        await repository.Add(asset);
        await transaction.Commit();
        db.ChangeTracker.Clear();

        var restored = Assert.Single(await repository.GetByOwner(PrivateAssetOwnerType.EmailCampaign, ownerId));
        Assert.Equal(asset.Id, restored.Id);
        Assert.Equal("banner", restored.ContentId);
        Assert.True(restored.IsInline);
        Assert.Equal(4, restored.SizeInBytes);
        Assert.Empty(await repository.GetByOwner((PrivateAssetOwnerType)(-1), ownerId));
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("Scheduled")]
    [InlineData("Sent")]
    public async Task upload_does_not_modify_owner_or_depend_on_campaign_state(string state)
    {
        await using var factory = new InMemoryDatabaseFactory();
        await using var db = await factory.CreateSelfServiceDbContext();
        var owner = Draft();
        if (state == "Scheduled")
            owner.MarkAsScheduled();
        else if (state == "Sent")
            owner.MarkAsSent();
        var modifiedAt = owner.ModifiedAt;
        var modifiedBy = owner.ModifiedBy;
        db.EmailCampaigns.Add(owner);
        await db.SaveChangesAsync();
        var storage = new Mock<IPrivateAssetService>();
        storage
            .Setup(service =>
                service.UploadAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("private-assets/test/banner.png");
        var service = Service(db, storage);
        var asset = await service.Upload(
            owner.Id,
            PrivateAssetOwnerType.EmailCampaign,
            "banner",
            true,
            "banner.png",
            "image/png",
            4,
            new MemoryStream([1, 2, 3, 4]),
            Editor
        );

        db.ChangeTracker.Clear();
        var afterUpload = await db.EmailCampaigns.FindAsync(owner.Id);
        Assert.Equal(state, afterUpload!.Status.ToString());
        Assert.Equal(modifiedAt, afterUpload.ModifiedAt);
        Assert.Equal(modifiedBy, afterUpload.ModifiedBy);
        Assert.Single(db.PrivateAssets);

        Assert.Equal(asset.Id, Assert.Single(db.PrivateAssets).Id);
    }

    [Fact]
    public async Task content_id_uniqueness_is_scoped_to_owner_and_enforced_by_database()
    {
        await using var factory = new InMemoryDatabaseFactory();
        await using var db = await factory.CreateSelfServiceDbContext();
        var owner = Guid.NewGuid();
        PrivateAsset Asset(Guid ownerId) =>
            A
                .PrivateAsset.WithStorageKey("private-assets/test/banner.png")
                .WithFileName("banner.png")
                .WithOwnerId(ownerId)
                .WithContentId("banner")
                .Build();
        db.PrivateAssets.AddRange(Asset(owner), Asset(Guid.NewGuid()));
        await db.SaveChangesAsync();
        db.PrivateAssets.Add(Asset(owner));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task user_without_upload_permission_is_denied_before_storage()
    {
        await using var factory = new InMemoryDatabaseFactory();
        await using var db = await factory.CreateSelfServiceDbContext();
        var storage = new Mock<IPrivateAssetService>(MockBehavior.Strict);
        var user = new PortalUser(UserId.Parse("reader@dfds.com"), []);
        var authorization = Mock.Of<IAuthorizationService>(service =>
            service.CanCreatePrivateAsset(It.IsAny<PortalUser>()) == false
        );

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () =>
                Service(db, storage, authorization: authorization)
                    .Upload(
                        Guid.NewGuid(),
                        PrivateAssetOwnerType.EmailCampaign,
                        "banner",
                        true,
                        "banner.png",
                        "image/png",
                        4,
                        new MemoryStream([1, 2, 3, 4]),
                        user
                    )
        );
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task storage_failure_does_not_write_metadata()
    {
        await using var factory = new InMemoryDatabaseFactory();
        await using var db = await factory.CreateSelfServiceDbContext();
        var owner = Draft();
        db.EmailCampaigns.Add(owner);
        await db.SaveChangesAsync();
        var storage = new Mock<IPrivateAssetService>();
        storage
            .Setup(service =>
                service.UploadAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new IOException("Storage unavailable"));

        await Assert.ThrowsAsync<IOException>(
            () =>
                Service(db, storage)
                    .Upload(
                        owner.Id,
                        PrivateAssetOwnerType.EmailCampaign,
                        "banner",
                        true,
                        "banner.png",
                        "image/png",
                        4,
                        new MemoryStream([1, 2, 3, 4]),
                        Editor
                    )
        );
        db.ChangeTracker.Clear();
        Assert.Empty(db.PrivateAssets);
    }
}
