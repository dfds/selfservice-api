namespace SelfService.Infrastructure.Images;

public interface IImageObjectStorage
{
    Task PutAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
    string GetPresignedReadUrl(string objectKey, TimeSpan validFor);
}