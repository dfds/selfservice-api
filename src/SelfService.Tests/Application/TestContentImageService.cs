using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Images;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.Application;

public class TestContentImageService
{
    [Fact]
    public async Task UploadAsync_ValidPng_PersistsMetadataAndUploadsObject()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var owner = new NewsItem(
            new NewsItemId(),
            "News",
            "Body",
            DateTime.UtcNow.AddDays(1),
            false,
            "tester",
            DateTime.UtcNow
        );
        dbContext.NewsItems.Add(owner);
        await dbContext.SaveChangesAsync();

        var storage = new FakeImageObjectStorage();
        var service = CreateService(dbContext, storage);
        var uploaded = await service.UploadAsync(
            ContentImageOwnerType.NewsItem,
            owner.Id,
            "folder\\image.png",
            new MemoryStream([137, 80, 78, 71, 13, 10, 26, 10, 1, 2]),
            10,
            CancellationToken.None
        );

        var saved = Assert.Single(dbContext.ContentImages);
        Assert.Equal(ContentImageOwnerType.NewsItem, saved.OwnerType);
        Assert.Equal((Guid)owner.Id, saved.OwnerId);
        Assert.Equal("image.png", saved.FileName);
        Assert.Equal("image/png", saved.ContentType);
        Assert.Equal(10, saved.Length);
        Assert.Equal($"content-image:{saved.Id:D}", uploaded.Reference);
        Assert.Single(storage.UploadedObjects);

        Assert.StartsWith(
            $"https://bucket.s3.us-east-1.amazonaws.com/{saved.ObjectKey}?X-Amz-Signature=",
            uploaded.Url
        );
        Assert.True(uploaded.UrlExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task UploadAsync_InvalidSignature_RejectsBeforeStorage()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var owner = new NewsItem(
            new NewsItemId(),
            "News",
            "Body",
            DateTime.UtcNow.AddDays(1),
            false,
            "tester",
            DateTime.UtcNow
        );
        dbContext.NewsItems.Add(owner);
        await dbContext.SaveChangesAsync();

        var storage = new FakeImageObjectStorage();
        var service = CreateService(dbContext, storage);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadAsync(
                ContentImageOwnerType.NewsItem,
                owner.Id,
                "not-an-image.png",
                new MemoryStream([1, 2, 3, 4]),
                4,
                CancellationToken.None
            )
        );

        Assert.Empty(dbContext.ContentImages);
        Assert.Empty(storage.UploadedObjects);
    }

    [Fact]
    public async Task UploadAsync_MissingOwner_RejectsBeforeStorage()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var storage = new FakeImageObjectStorage();
        var service = CreateService(dbContext, storage);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UploadAsync(
                ContentImageOwnerType.Event,
                Guid.NewGuid(),
                "image.png",
                new MemoryStream([137, 80, 78, 71, 13, 10, 26, 10]),
                8,
                CancellationToken.None
            )
        );

        Assert.Empty(storage.UploadedObjects);
    }

    [Fact]
    public async Task DeleteAsync_RemovesMetadataAndObject()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var owner = new NewsItem(
            new NewsItemId(),
            "News",
            "Body",
            DateTime.UtcNow.AddDays(1),
            false,
            "tester",
            DateTime.UtcNow
        );
        dbContext.NewsItems.Add(owner);
        await dbContext.SaveChangesAsync();

        var storage = new FakeImageObjectStorage();
        var service = CreateService(dbContext, storage);
        var uploaded = await service.UploadAsync(
            ContentImageOwnerType.NewsItem,
            owner.Id,
            "image.png",
            new MemoryStream([137, 80, 78, 71, 13, 10, 26, 10]),
            8,
            CancellationToken.None
        );

        Assert.True(await service.DeleteAsync(uploaded.Id, CancellationToken.None));

        Assert.Empty(dbContext.ContentImages);
        Assert.Empty(storage.UploadedObjects);
    }

    [Fact]
    public async Task ResolveReferencesAsync_ReplacesOnlyImagesOwnedByContentItem()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var owner = new NewsItem(
            new NewsItemId(),
            "News",
            "Body",
            DateTime.UtcNow.AddDays(1),
            false,
            "tester",
            DateTime.UtcNow
        );
        dbContext.NewsItems.Add(owner);
        await dbContext.SaveChangesAsync();

        var storage = new FakeImageObjectStorage();
        var service = CreateService(dbContext, storage);
        var uploaded = await service.UploadAsync(
            ContentImageOwnerType.NewsItem,
            owner.Id,
            "image.png",
            new MemoryStream([137, 80, 78, 71, 13, 10, 26, 10]),
            8,
            CancellationToken.None
        );
        var missingReference = $"content-image:{Guid.NewGuid():D}";
        var content = $"<img src=\"{uploaded.Reference}\"><p>{missingReference}</p>";

        var resolved = await service.ResolveReferencesAsync(
            ContentImageOwnerType.NewsItem,
            (Guid)owner.Id,
            content,
            CancellationToken.None
        );
        var referenced = await service.GetReferencedAsync(
            ContentImageOwnerType.NewsItem,
            (Guid)owner.Id,
            content,
            CancellationToken.None
        );

        Assert.Contains(
            $"https://bucket.s3.us-east-1.amazonaws.com/{uploaded.ObjectKey}?X-Amz-Signature=",
            resolved
        );
        Assert.Contains(missingReference, resolved);
        Assert.Single(referenced);
        Assert.Equal(uploaded.Id, referenced[0].Id);
    }

    [Fact]
    public async Task RestoreReferencesAsync_ConvertsOwnedImageUrlsToMarkersAndLeavesExternalUrlsAlone()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var owner = new NewsItem(
            new NewsItemId(),
            "News",
            "Body",
            DateTime.UtcNow.AddDays(1),
            false,
            "tester",
            DateTime.UtcNow
        );
        dbContext.NewsItems.Add(owner);
        await dbContext.SaveChangesAsync();

        var storage = new FakeImageObjectStorage();
        var service = CreateService(dbContext, storage);
        var uploaded = await service.UploadAsync(
            ContentImageOwnerType.NewsItem,
            owner.Id,
            "image.png",
            new MemoryStream([137, 80, 78, 71, 13, 10, 26, 10]),
            8,
            CancellationToken.None
        );
        var externalUrlWithSameObjectKey = $"https://external.test/{uploaded.ObjectKey}";
        var contentWithSignedUrl = $"<img src=\"{uploaded.Url}\"><img src=\"{externalUrlWithSameObjectKey}\">";

        var restored = await service.RestoreReferencesAsync(
            ContentImageOwnerType.NewsItem,
            (Guid)owner.Id,
            contentWithSignedUrl,
            CancellationToken.None
        );

        Assert.Contains($"src=\"{uploaded.Reference}\"", restored);
        Assert.Contains(externalUrlWithSameObjectKey, restored);
    }

    [Fact]
    public async Task DeleteAsync_WhenStorageFails_HidesPendingImageAndCanBeRetried()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var owner = new NewsItem(
            new NewsItemId(),
            "News",
            "Body",
            DateTime.UtcNow.AddDays(1),
            false,
            "tester",
            DateTime.UtcNow
        );
        dbContext.NewsItems.Add(owner);
        await dbContext.SaveChangesAsync();

        var storage = new FakeImageObjectStorage { FailNextDelete = true };
        var service = CreateService(dbContext, storage);
        var uploaded = await service.UploadAsync(
            ContentImageOwnerType.NewsItem,
            owner.Id,
            "image.png",
            new MemoryStream([137, 80, 78, 71, 13, 10, 26, 10]),
            8,
            CancellationToken.None
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(uploaded.Id, CancellationToken.None));

        Assert.Empty(
            await service.GetReferencedAsync(
                ContentImageOwnerType.NewsItem,
                (Guid)owner.Id,
                uploaded.Reference,
                CancellationToken.None
            )
        );
        Assert.True(await service.DeleteAsync(uploaded.Id, CancellationToken.None));
        Assert.Empty(dbContext.ContentImages);
        Assert.Empty(storage.UploadedObjects);
    }

    [Fact]
    public async Task DeleteForOwnerAsync_RemovesOnlyImagesForThatOwner()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var owner = new NewsItem(
            new NewsItemId(),
            "News",
            "Body",
            DateTime.UtcNow.AddDays(1),
            false,
            "tester",
            DateTime.UtcNow
        );
        var otherOwner = new NewsItem(
            new NewsItemId(),
            "Other news",
            "Body",
            DateTime.UtcNow.AddDays(1),
            false,
            "tester",
            DateTime.UtcNow
        );
        dbContext.NewsItems.AddRange(owner, otherOwner);
        await dbContext.SaveChangesAsync();

        var storage = new FakeImageObjectStorage();
        var service = CreateService(dbContext, storage);
        var content = new MemoryStream([137, 80, 78, 71, 13, 10, 26, 10]);
        await service.UploadAsync(
            ContentImageOwnerType.NewsItem,
            owner.Id,
            "owned.png",
            content,
            8,
            CancellationToken.None
        );
        await service.UploadAsync(
            ContentImageOwnerType.NewsItem,
            otherOwner.Id,
            "other.png",
            new MemoryStream([137, 80, 78, 71, 13, 10, 26, 10]),
            8,
            CancellationToken.None
        );

        await service.DeleteForOwnerAsync(ContentImageOwnerType.NewsItem, (Guid)owner.Id, CancellationToken.None);

        Assert.Single(storage.UploadedObjects);
        Assert.Equal((Guid)otherOwner.Id, Assert.Single(dbContext.ContentImages).OwnerId);
    }

    private static ContentImageService CreateService(
        SelfServiceDbContext dbContext,
        FakeImageObjectStorage storage
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SS_IMAGES_MAX_UPLOAD_BYTES"] = "1024" })
            .Build();
        return new ContentImageService(dbContext, storage, configuration, NullLogger<ContentImageService>.Instance);
    }

    private sealed class FakeImageObjectStorage : IImageObjectStorage
    {
        public List<string> UploadedObjects { get; } = [];
        private readonly Dictionary<string, byte[]> _objects = [];
        public bool FailNextDelete { get; set; }

        public async Task PutAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken)
        {
            UploadedObjects.Add(objectKey);
            await using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            _objects[objectKey] = buffer.ToArray();
        }

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
        {
            if (FailNextDelete)
            {
                FailNextDelete = false;
                throw new InvalidOperationException("Simulated S3 delete failure.");
            }

            UploadedObjects.Remove(objectKey);
            _objects.Remove(objectKey);
            return Task.CompletedTask;
        }

        public string GetPresignedReadUrl(string objectKey, TimeSpan validFor) =>
            $"https://bucket.s3.us-east-1.amazonaws.com/{objectKey}?X-Amz-Signature=test&expires={DateTime.UtcNow.Add(validFor):O}";
    }
}