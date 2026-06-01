using System.Text.Json;
using System.Text.RegularExpressions;
using SelfService.Domain.Services;

namespace SelfService.Infrastructure.Persistence;

public class TemplateRenderingService : ITemplateRenderingService
{
    private static readonly Regex TokenRegex = new(@"\{\{\s*([^}]+?)\s*\}\}", RegexOptions.Compiled);

    private abstract record VariableEntry(string Name, string Description, string Entity, string Example, bool Hidden);

    private sealed record StaticVariable(
        string Name,
        string Description,
        string Entity,
        string Example,
        Func<TemplateRenderContext, string> Resolve,
        bool Hidden = false
    ) : VariableEntry(Name, Description, Entity, Example, Hidden);

    private sealed record PatternVariable(
        string Name,
        string Description,
        string Entity,
        string Example,
        Regex Regex,
        Func<Match, TemplateRenderContext, string?> Resolve,
        string? FallbackOnMiss = null,
        bool Hidden = false
    ) : VariableEntry(Name, Description, Entity, Example, Hidden);

    private static readonly VariableEntry[] Variables =
    {
        new StaticVariable(
            "Capability.Id",
            "Capability ID slug",
            "Capability",
            "my-capability-abc12",
            ctx => ctx.Capability.Id.ToString()
        ),
        new StaticVariable(
            "Capability.Name",
            "Display name",
            "Capability",
            "My Capability",
            ctx => ctx.Capability.Name ?? ""
        ),
        new StaticVariable(
            "Capability.Description",
            "Description text",
            "Capability",
            "A description...",
            ctx => ctx.Capability.Description ?? ""
        ),
        new StaticVariable(
            "Capability.Status",
            "Active or Pending Deletion",
            "Capability",
            "Active",
            ctx => ctx.Capability.Status.ToString()
        ),
        new StaticVariable(
            "Capability.CreatedAt",
            "Creation date (yyyy-MM-dd)",
            "Capability",
            "2024-01-15",
            ctx => ctx.Capability.CreatedAt.ToString("yyyy-MM-dd")
        ),
        new StaticVariable(
            "Capability.CreatedBy",
            "Creator user ID",
            "Capability",
            "user@dfds.com",
            ctx => ctx.Capability.CreatedBy ?? ""
        ),
        new StaticVariable(
            "Capability.RequirementScore",
            "Compliance score (0-100)",
            "Capability",
            "85",
            ctx => ctx.Capability.RequirementScore?.ToString("0") ?? "N/A"
        ),
        new StaticVariable(
            "Capability.MemberCount",
            "Number of members",
            "Capability",
            "12",
            ctx => ctx.MemberCount.ToString()
        ),
        new StaticVariable(
            "Member.DisplayName",
            "Recipient display name",
            "Member",
            "Jane Doe",
            ctx => ctx.Member?.DisplayName ?? "[Member Name]"
        ),
        new StaticVariable(
            "Member.Email",
            "Recipient email address",
            "Member",
            "jane.doe@dfds.com",
            ctx => ctx.Member?.Email ?? "[Member Email]"
        ),
        new StaticVariable(
            "Campaign.Name",
            "Campaign name",
            "Campaign",
            "Q1 Migration Notice",
            ctx => ctx.CampaignName
        ),
        new StaticVariable(
            "Date.Today",
            "Current date (yyyy-MM-dd)",
            "Date",
            "2024-01-15",
            _ => DateTime.UtcNow.ToString("yyyy-MM-dd")
        ),
        new StaticVariable("Date.Year", "Current year", "Date", "2024", _ => DateTime.UtcNow.Year.ToString()),
        new PatternVariable(
            "Requirement.<id>",
            "Individual requirement score (0-100). Replace <id> with: mandatory_tags, external_secrets, irsa, k8s-probes, ecr-pull",
            "Requirement",
            "85",
            new Regex(@"^Requirement\.(?<id>[^.]+)$", RegexOptions.Compiled),
            (m, ctx) => ResolveRequirementScore(ctx, m.Groups["id"].Value),
            FallbackOnMiss: "N/A"
        ),
        new PatternVariable(
            "Requirement.<id>.DisplayName",
            "Requirement display name",
            "Requirement",
            "Use of Mandatory Tags",
            new Regex(@"^Requirement\.(?<id>[^.]+)\.DisplayName$", RegexOptions.Compiled),
            (m, ctx) => ResolveRequirementDisplayName(ctx, m.Groups["id"].Value),
            FallbackOnMiss: "N/A"
        ),
        new PatternVariable(
            "Requirement.<id>.HelpUrl",
            "Requirement help URL",
            "Requirement",
            "https://wiki.dfds.cloud/...",
            new Regex(@"^Requirement\.(?<id>[^.]+)\.HelpUrl$", RegexOptions.Compiled),
            (m, ctx) => ResolveRequirementHelpUrl(ctx, m.Groups["id"].Value),
            FallbackOnMiss: "N/A"
        ),
        // Catch-all: preserves the original "any unmatched Requirement.* → N/A" behavior.
        new PatternVariable(
            "Requirement.<unknown>",
            "",
            "Requirement",
            "",
            new Regex(@"^Requirement\..+$", RegexOptions.Compiled),
            (_, _) => null,
            FallbackOnMiss: "N/A",
            Hidden: true
        ),
        new StaticVariable(
            "Aws.AccountId",
            "AWS account number (12-digit)",
            "AwsAccount",
            "123456789012",
            ctx => ctx.AwsAccount?.Registration.AccountId?.ToString() ?? "N/A"
        ),
        new StaticVariable(
            "Aws.Status",
            "AWS account status",
            "AwsAccount",
            "Completed",
            ctx => ctx.AwsAccount?.Status.ToString() ?? "N/A"
        ),
        new StaticVariable(
            "Aws.Namespace",
            "Kubernetes namespace linked to AWS account",
            "AwsAccount",
            "my-capability-abc12",
            ctx => ctx.AwsAccount?.KubernetesLink.Namespace ?? "N/A"
        ),
        new StaticVariable(
            "Aws.RoleEmail",
            "AWS account role email",
            "AwsAccount",
            "aws.123456789012@dfds.com",
            ctx => ctx.AwsAccount?.Registration.RoleEmail ?? "N/A"
        ),
        new StaticVariable(
            "Azure.ResourceCount",
            "Number of Azure resource groups",
            "AzureResource",
            "2",
            ctx => ctx.AzureResources.Count.ToString()
        ),
        new StaticVariable(
            "Azure.Environments",
            "Comma-separated Azure environments",
            "AzureResource",
            "dev, prod",
            ctx =>
                ctx.AzureResources.Count > 0
                    ? string.Join(", ", ctx.AzureResources.Select(r => r.Environment).OrderBy(e => e))
                    : "None"
        ),
        new PatternVariable(
            "Azure.<env>.Id",
            "Resource group ID for a specific environment (e.g. Azure.dev.Id)",
            "AzureResource",
            "a1b2c3d4-e5f6-...",
            new Regex(@"^Azure\.(?<env>[^.]+)\.Id$", RegexOptions.Compiled),
            (m, ctx) => ctx.AzureResources.FirstOrDefault(r => r.Environment == m.Groups["env"].Value)?.Id.ToString()
        ),
        new StaticVariable(
            "MembershipApplications.PendingCount",
            "Number of pending membership applications",
            "MembershipApplication",
            "3",
            ctx => ctx.PendingMembershipApplicationCount.ToString()
        ),
        // Hidden: not advertised by /email-campaigns/variables but supported by the renderer.
        new PatternVariable(
            "Metadata.<key>",
            "",
            "Capability",
            "",
            new Regex(@"^Metadata\.(?<key>.+)$", RegexOptions.Compiled),
            (m, ctx) => LookupMetadata(ctx.Capability.JsonMetadata, m.Groups["key"].Value),
            Hidden: true
        ),
    };

    private static readonly Dictionary<string, StaticVariable> ByName = Variables
        .OfType<StaticVariable>()
        .ToDictionary(v => v.Name);

    private static readonly PatternVariable[] Patterns = Variables.OfType<PatternVariable>().ToArray();

    public string RenderTemplate(string template, TemplateRenderContext context) =>
        TokenRegex.Replace(template, m => Resolve(m.Groups[1].Value, context) ?? m.Value);

    public IReadOnlyList<TemplateVariable> GetVariableDefinitions() =>
        Variables
            .Where(v => !v.Hidden)
            .Select(v => new TemplateVariable
            {
                Name = v.Name,
                Description = v.Description,
                Entity = v.Entity,
                Example = v.Example,
            })
            .ToList();

    private static string? Resolve(string name, TemplateRenderContext context)
    {
        if (ByName.TryGetValue(name, out var def))
            return def.Resolve(context);

        foreach (var p in Patterns)
        {
            var match = p.Regex.Match(name);
            if (!match.Success)
                continue;
            return p.Resolve(match, context) ?? p.FallbackOnMiss;
        }

        return null;
    }

    private static string? ResolveRequirementScore(TemplateRenderContext ctx, string id) =>
        ctx.RequirementScores.FirstOrDefault(s => s.RequirementId == id)?.Value.ToString("0");

    private static string? ResolveRequirementDisplayName(TemplateRenderContext ctx, string id)
    {
        var metric = ctx.RequirementScores.FirstOrDefault(s => s.RequirementId == id);
        return metric is null ? null : (metric.DisplayName ?? metric.RequirementId);
    }

    private static string? ResolveRequirementHelpUrl(TemplateRenderContext ctx, string id)
    {
        var metric = ctx.RequirementScores.FirstOrDefault(s => s.RequirementId == id);
        return metric is null ? null : (metric.HelpUrl ?? "");
    }

    private static string? LookupMetadata(string? jsonMetadata, string key)
    {
        if (string.IsNullOrEmpty(jsonMetadata))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(jsonMetadata);
            if (!doc.RootElement.TryGetProperty(key, out var prop))
                return null;
            return prop.ValueKind == JsonValueKind.String ? prop.GetString() ?? "" : prop.GetRawText();
        }
        catch
        {
            return null;
        }
    }
}
