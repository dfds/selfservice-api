using SelfService.Application;
using SelfService.Domain.Models;

namespace SelfService.Tests.Application;

public class TestEmailInlineImageResolver
{
    [Fact]
    public void Resolve_EmbeddedCampaignImage_RewritesOnlyOwnedImageAndReturnsReference()
    {
        var imageId = Guid.NewGuid();
        var campaignImage = new ContentImageInfo(
            imageId,
            ContentImageOwnerType.EmailCampaign,
            Guid.NewGuid(),
            "diagram.png",
            "image/png",
            128,
            "private-object-key",
            $"content-image:{imageId:D}",
            "https://s3.example.test/private-object-key?sig=temporary",
            DateTime.UtcNow.AddMinutes(15),
            DateTime.UtcNow
        );
        var newsImage = campaignImage with { OwnerType = ContentImageOwnerType.NewsItem };
        var html = $"<p>{campaignImage.Reference}</p><a href=\"{campaignImage.Reference}\">Open image</a><img src=\"{campaignImage.Reference}\"><img src=\"https://external.test/image.png\">";

        var (resolvedHtml, inlineImages) = EmailInlineImageResolver.Resolve(html, [campaignImage, newsImage]);

        Assert.Contains($"src=\"cid:image-{imageId:N}\"", resolvedHtml);
        Assert.Contains($"<p>{campaignImage.Reference}</p>", resolvedHtml);
        Assert.Contains($"href=\"{campaignImage.Reference}\"", resolvedHtml);
        Assert.DoesNotContain("s3.example.test", resolvedHtml);
        Assert.Contains("https://external.test/image.png", resolvedHtml);
        var inlineImage = Assert.Single(inlineImages);
        Assert.Equal($"image-{imageId:N}", inlineImage.ContentId);
        Assert.Equal("private-object-key", inlineImage.ObjectKey);
        Assert.Equal("image/png", inlineImage.ContentType);
        Assert.Equal("diagram.png", inlineImage.FileName);
    }

    [Fact]
    public void Resolve_UnusedCampaignImage_DoesNotAttachIt()
    {
        var image = new ContentImageInfo(
            Guid.NewGuid(),
            ContentImageOwnerType.EmailCampaign,
            Guid.NewGuid(),
            "diagram.png",
            "image/png",
            128,
            "private-object-key",
            $"content-image:{Guid.NewGuid():D}",
            "https://s3.example.test/private-object-key?sig=temporary",
            DateTime.UtcNow.AddMinutes(15),
            DateTime.UtcNow
        );

        var (resolvedHtml, inlineImages) = EmailInlineImageResolver.Resolve("<p>No image used</p>", [image]);

        Assert.Equal("<p>No image used</p>", resolvedHtml);
        Assert.Empty(inlineImages);
    }
}