using SelfService.Domain.Events;
using SelfService.Domain.Models;
using System.Text.RegularExpressions;

namespace SelfService.Application;

public static class EmailInlineImageResolver
{
    public static (string HtmlBody, List<InlineEmailImage> InlineImages) Resolve(
        string htmlBody,
        IEnumerable<ContentImageInfo> images
    )
    {
        var resolvedHtml = htmlBody;
        var inlineImages = new List<InlineEmailImage>();

        foreach (var image in images)
        {
            if (image.OwnerType != ContentImageOwnerType.EmailCampaign)
            {
                continue;
            }

            var contentId = $"image-{image.Id:N}";
            var pattern = $"(?<prefix><img\\b[^>]*?\\bsrc\\s*=\\s*)(?<quote>[\"']){Regex.Escape(image.Reference)}\\k<quote>";
            var didReplace = false;
            resolvedHtml = Regex.Replace(
                resolvedHtml,
                pattern,
                match =>
                {
                    didReplace = true;
                    return $"{match.Groups["prefix"].Value}{match.Groups["quote"].Value}cid:{contentId}{match.Groups["quote"].Value}";
                },
                RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant
            );

            if (didReplace)
            {
                inlineImages.Add(new InlineEmailImage(contentId, image.ObjectKey, image.ContentType, image.FileName));
            }
        }

        return (resolvedHtml, inlineImages);
    }
}