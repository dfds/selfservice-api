using Amazon.S3;
using Amazon.S3.Model;
using SelfService.Domain.Services;

namespace SelfService.Infrastructure.Storage;

public class S3PrivateAssetService : IPrivateAssetService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3PrivateAssetService(IAmazonS3 s3Client, PrivateAssetStorageOptions options)
    {
        _s3Client = s3Client;
        _bucketName = options.BucketName;
    }

    public async Task<string> UploadAsync(
        string fileName,
        string mimeType,
        Stream content,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        if (string.IsNullOrWhiteSpace(mimeType))
            throw new ArgumentException("Mime type is required.", nameof(mimeType));

        if (content is null)
            throw new ArgumentNullException(nameof(content));

        var storageKey = $"private-assets/{Guid.NewGuid():N}/{SanitizeFileName(fileName)}";

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = storageKey,
            InputStream = content,
            ContentType = mimeType,
            AutoCloseStream = true,
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
        return storageKey;
    }

    public async Task<string> GetPrivateUrlAsync(
        string storageKey,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key is required.", nameof(storageKey));

        var duration = ttl ?? TimeSpan.FromMinutes(5);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = storageKey,
            Expires = DateTime.UtcNow.Add(duration),
        };

        var url = _s3Client.GetPreSignedURL(request);

        // The SDK always presigns with https; downgrade to match an http endpoint (e.g. the local S3 mock).
        // Only the host is signed (SignedHeaders=host), so changing the scheme does not invalidate the signature.
        if (
            _s3Client.Config.ServiceURL?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true
            && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        )
        {
            url = string.Concat("http://", url.AsSpan("https://".Length));
        }

        return await Task.FromResult(url);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key is required.", nameof(storageKey));

        // S3 DeleteObject is idempotent: deleting a missing key still succeeds, so retries are safe.
        await _s3Client.DeleteObjectAsync(
            new DeleteObjectRequest { BucketName = _bucketName, Key = storageKey },
            cancellationToken
        );
    }

    private static string SanitizeFileName(string input)
    {
        var fileName = Path.GetFileName(input);
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "asset";

        return fileName.Replace(' ', '-');
    }
}
