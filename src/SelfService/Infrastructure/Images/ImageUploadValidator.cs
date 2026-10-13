namespace SelfService.Infrastructure.Images;

public static class ImageUploadValidator
{
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static bool TryDetectContentType(ReadOnlySpan<byte> content, out string contentType)
    {
        if (content.StartsWith(PngSignature))
        {
            contentType = "image/png";
            return true;
        }

        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
        {
            contentType = "image/jpeg";
            return true;
        }

        contentType = string.Empty;
        return false;
    }

    public static bool IsWithinSizeLimit(long length, long maximumSizeBytes) =>
        length > 0 && maximumSizeBytes > 0 && length <= maximumSizeBytes;
}