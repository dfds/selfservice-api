using System.Text.Json.Nodes;

namespace SelfService.Domain.Services;

/// <summary>
/// Single source of truth for evaluating a capability's mandatory-tag compliance from its
/// JSON metadata. Used by both the compliance endpoint (GET /compliance/capabilities/{id}) and the
/// email template engine so the Tags score is computed identically in both places.
/// </summary>
public static class TagComplianceEvaluator
{
    public const string RequirementId = "mandatory_tags";
    public const string CategoryName = "Tags";
    public const string DisplayName = "Tags for Capabilities";
    public const string HelpUrl = "https://wiki.dfds.cloud/en/playbooks/requirements/Mandatory-tags-for-capabilities";
    public const string Description = "Mandatory tags on a Capability level";

    public static readonly IReadOnlyList<string> RequiredTags = new[]
    {
        "dfds.cost.centre",
        "dfds.businessCapability",
        "dfds.env",
        "dfds.data.classification",
        "dfds.service.criticality",
        "dfds.service.availability",
    };

    public record TagResult(string Name, bool IsPresent, string? Value);

    public record TagEvaluation(IReadOnlyList<TagResult> Tags, double Score, bool IsCompliant);

    public static TagEvaluation Evaluate(string? jsonMetadata)
    {
        JsonObject? jsonObject = null;
        if (!string.IsNullOrEmpty(jsonMetadata))
        {
            jsonObject = JsonNode.Parse(jsonMetadata)?.AsObject();
        }

        var tags = new List<TagResult>();
        foreach (var tag in RequiredTags)
        {
            var value = jsonObject?[tag]?.ToString();
            var isPresent = !string.IsNullOrEmpty(value);
            tags.Add(new TagResult(tag, isPresent, isPresent ? value : null));
        }

        var presentCount = tags.Count(t => t.IsPresent);
        var score = (double)presentCount / tags.Count * 100;

        return new TagEvaluation(tags, score, presentCount == tags.Count);
    }
}
