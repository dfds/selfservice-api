using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using SelfService.Domain.Services;

namespace SelfService.Infrastructure.Storage;

public class S3PrivateAssetService : IPrivateAssetService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3PrivateAssetService(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName =
            configuration["SS_PRIVATE_ASSET_BUCKET"]
            ?? throw new InvalidOperationException("SS_PRIVATE_ASSET_BUCKET configuration is required but not set.");
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

    public async Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key is required.", nameof(storageKey));

        var response = await _s3Client.GetObjectAsync(_bucketName, storageKey, cancellationToken);
        var stream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(stream, cancellationToken);
        stream.Position = 0;
        return stream;
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

        return await Task.FromResult(_s3Client.GetPreSignedURL(request));
    }

    private static string SanitizeFileName(string input)
    {
        var fileName = Path.GetFileName(input);
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "asset";

        return fileName.Replace(' ', '-');
    }
}
