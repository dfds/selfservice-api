using System.Text.RegularExpressions;

namespace SelfService.Application;

public static class ContentImageReference
{
    private static readonly Regex ReferencePattern = new(
        @"content-image:(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
    );

    public static IReadOnlyList<Guid> FindIds(string content) => ReferencePattern
        .Matches(content)
        .Select(match => Guid.Parse(match.Groups["id"].Value))
        .Distinct()
        .ToList();

    public static string ReplaceWithUrls(string content, IReadOnlyDictionary<Guid, string> urlsById) =>
        ReferencePattern.Replace(
            content,
            match =>
            {
                var id = Guid.Parse(match.Groups["id"].Value);
                return urlsById.TryGetValue(id, out var url) ? url : match.Value;
            }
        );
}