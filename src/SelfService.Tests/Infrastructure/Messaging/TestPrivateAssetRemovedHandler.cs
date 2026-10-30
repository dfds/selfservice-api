using Dafda.Consuming;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SelfService.Domain.Events;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Messaging;

namespace SelfService.Tests.Infrastructure.Messaging;

public class TestPrivateAssetRemovedHandler
{
    [Fact]
    public async Task deletes_the_stored_object_for_the_removed_asset()
    {
        var storage = new Mock<IPrivateAssetService>();
        var handler = new PrivateAssetRemovedHandler(NullLogger<PrivateAssetRemovedHandler>.Instance, storage.Object);

        await handler.Handle(
            new PrivateAssetRemoved { AssetId = "asset-1", StorageKey = "private-assets/abc/banner.png" },
            new Mock<MessageHandlerContext>().Object
        );

        storage.Verify(
            service => service.DeleteAsync("private-assets/abc/banner.png", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task skips_deletion_when_storage_key_is_missing()
    {
        var storage = new Mock<IPrivateAssetService>(MockBehavior.Strict);
        var handler = new PrivateAssetRemovedHandler(NullLogger<PrivateAssetRemovedHandler>.Instance, storage.Object);

        await handler.Handle(
            new PrivateAssetRemoved { AssetId = "asset-1", StorageKey = null },
            new Mock<MessageHandlerContext>().Object
        );

        storage.VerifyNoOtherCalls();
    }
}
