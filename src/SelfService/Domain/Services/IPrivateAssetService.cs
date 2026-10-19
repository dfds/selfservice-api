namespace SelfService.Domain.Services;

public interface IPrivateAssetService
{
    Task<string> UploadAsync(
        string fileName,
        string mimeType,
        Stream content,
        CancellationToken cancellationToken = default
    );

    Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default);

    Task<string> GetPrivateUrlAsync(
        string storageKey,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default
    );
}
