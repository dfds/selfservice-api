using Amazon.S3;
using Amazon.S3.Model;

namespace SelfService.Infrastructure.Images;

public sealed class S3ImageObjectStorage : IImageObjectStorage
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;

    public S3ImageObjectStorage(IAmazonS3 s3, IConfiguration configuration)
    {
        _s3 = s3;
        _bucketName = configuration["SS_IMAGES_S3_BUCKET"] ?? string.Empty;
    }

    public async Task PutAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken
    )
    {
        EnsureBucketConfigured();
        await _s3.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                InputStream = content,
                ContentType = contentType,
            },
            cancellationToken
        );
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        EnsureBucketConfigured();
        await _s3.DeleteObjectAsync(_bucketName, objectKey, cancellationToken);
    }

    public string GetPresignedReadUrl(string objectKey, TimeSpan validFor)
    {
        EnsureBucketConfigured();
        return _s3.GetPreSignedURL(
            new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.Add(validFor),
            }
        );
    }

    private void EnsureBucketConfigured()
    {
        if (string.IsNullOrWhiteSpace(_bucketName))
        {
            throw new InvalidOperationException("SS_IMAGES_S3_BUCKET must be configured.");
        }
    }
}