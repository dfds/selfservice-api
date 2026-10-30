using Dafda.Consuming;
using SelfService.Domain.Events;
using SelfService.Domain.Services;

namespace SelfService.Infrastructure.Messaging;

public class PrivateAssetRemovedHandler : IMessageHandler<PrivateAssetRemoved>
{
    private readonly ILogger<PrivateAssetRemovedHandler> _logger;
    private readonly IPrivateAssetService _storage;

    public PrivateAssetRemovedHandler(ILogger<PrivateAssetRemovedHandler> logger, IPrivateAssetService storage)
    {
        _logger = logger;
        _storage = storage;
    }

    public async Task Handle(PrivateAssetRemoved message, MessageHandlerContext context)
    {
        using var _ = _logger.BeginScope(
            "Handling {MessageType} on {ImplementationType} with {CorrelationId} and {CausationId}",
            context.MessageType,
            GetType().Name,
            context.CorrelationId,
            context.CausationId
        );

        if (string.IsNullOrWhiteSpace(message.StorageKey))
        {
            _logger.LogWarning(
                "PrivateAssetRemoved for asset {AssetId} had no storage key - nothing to delete",
                message.AssetId
            );
            return;
        }

        await _storage.DeleteAsync(message.StorageKey);
        _logger.LogInformation(
            "Deleted stored object {StorageKey} for removed asset {AssetId}",
            message.StorageKey,
            message.AssetId
        );
    }
}
