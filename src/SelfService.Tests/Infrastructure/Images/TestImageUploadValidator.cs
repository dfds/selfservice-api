using SelfService.Infrastructure.Images;

namespace SelfService.Tests.Infrastructure.Images;

public class TestImageUploadValidator
{
    [Theory]
    [InlineData(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, "image/png")]
    [InlineData(new byte[] { 255, 216, 255, 224 }, "image/jpeg")]
    public void TryDetectContentType_RecognizedSignature_ReturnsContentType(byte[] content, string expected)
    {
        var found = ImageUploadValidator.TryDetectContentType(content, out var contentType);

        Assert.True(found);
        Assert.Equal(expected, contentType);
    }

    [Theory]
    [InlineData(new byte[] { 0, 1, 2 })]
    [InlineData(new byte[] { })]
    public void TryDetectContentType_UnknownSignature_ReturnsFalse(byte[] content)
    {
        var found = ImageUploadValidator.TryDetectContentType(content, out var contentType);

        Assert.False(found);
        Assert.Empty(contentType);
    }

    [Theory]
    [InlineData(1, 10, true)]
    [InlineData(10, 10, true)]
    [InlineData(11, 10, false)]
    [InlineData(0, 10, false)]
    [InlineData(1, 0, false)]
    public void IsWithinSizeLimit_UsesInclusivePositiveLimit(long length, long maximumSizeBytes, bool expected)
    {
        Assert.Equal(expected, ImageUploadValidator.IsWithinSizeLimit(length, maximumSizeBytes));
    }
}