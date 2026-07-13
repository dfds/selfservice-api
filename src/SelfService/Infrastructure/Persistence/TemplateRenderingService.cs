using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SelfService.Domain.Models;
using SelfService.Domain.Services;

namespace SelfService.Infrastructure.Persistence;

public class TemplateRenderingService : ITemplateRenderingService
{
    private static readonly Regex TokenRegex = new(@"\{\{\s*([^}]+?)\s*\}\}", RegexOptions.Compiled);

    // Block syntax: {{#each User.Capabilities}} ... {{/each}}.
    // Non-greedy body so multiple blocks in the same template render independently.
    // Singleline so the body can span newlines (HTML email bodies frequently do).
    private static readonly Regex UserCapabilitiesBlockRegex = new(
        @"\{\{\s*#each\s+User\.Capabilities\s*\}\}(?<body>.*?)\{\{\s*/each\s*\}\}",
        RegexOptions.Compiled | RegexOptions.Singleline
    );

    private readonly string _portalBaseUrl;
    private readonly VariableEntry[] _variables;
    private readonly Dictionary<string, StaticVariable> _byName;
    private readonly PatternVariable[] _patterns;
    private readonly ILogger<TemplateRenderingService> _logger;

    [Flags]
    private enum AppliesTo
    {
        Capability = 1,
        User = 2,
        Both = Capability | User,
    }

    /// <summary>
    /// Where a variable resolves. <see cref="PerCapability"/> variables describe a capability:
    /// they resolve directly in Capability-targeted campaigns, but only inside a
    /// {{#each User.Capabilities}} block in User-targeted campaigns. <see cref="TopLevel"/>
    /// variables resolve at the campaign/recipient root (Member, User, Campaign, Date).
    /// </summary>
    private enum VarScope
    {
        TopLevel,
        PerCapability,
    }

    private abstract record VariableEntry(
        string Name,
        string Description,
        string Entity,
        string Example,
        bool Hidden,
        AppliesTo Applies,
        VarScope Scope
    );

    private sealed record StaticVariable(
        string Name,
        string Description,
        string Entity,
        string Example,
        Func<TemplateRenderContext, string> Resolve,
        AppliesTo Applies = AppliesTo.Capability,
        bool Hidden = false,
        VarScope Scope = VarScope.PerCapability
    ) : VariableEntry(Name, Description, Entity, Example, Hidden, Applies, Scope);

    private sealed record PatternVariable(
        string Name,
        string Description,
        string Entity,
        string Example,
        Regex Regex,
        Func<Match, TemplateRenderContext, string?> Resolve,
        string? FallbackOnMiss = null,
        AppliesTo Applies = AppliesTo.Capability,
        bool Hidden = false,
        VarScope Scope = VarScope.PerCapability
    ) : VariableEntry(Name, Description, Entity, Example, Hidden, Applies, Scope);

    public TemplateRenderingService(IConfiguration configuration, ILogger<TemplateRenderingService> logger)
    {
        _logger = logger;
        _portalBaseUrl =
            configuration["SS_PORTAL_BASE_URL"]
            ?? throw new InvalidOperationException("SS_PORTAL_BASE_URL configuration is required but not set.");
        _logger.LogInformation(
            "TemplateRenderingService initialized with portal base URL: {PortalBaseUrl}",
            _portalBaseUrl
        );
        _variables = InitializeVariables();
        _byName = _variables.OfType<StaticVariable>().ToDictionary(v => v.Name);
        _patterns = _variables.OfType<PatternVariable>().ToArray();
    }

    private VariableEntry[] InitializeVariables()
    {
        return new VariableEntry[]
        {
            // --- Capability-targeted variables ---
            new StaticVariable(
                "Capability.Id",
                "Capability ID slug",
                "Capability",
                "my-capability-abc12",
                ctx => ctx.Capability?.Id.ToString() ?? ""
            ),
            new StaticVariable(
                "Capability.Name",
                "Display name",
                "Capability",
                "My Capability",
                ctx => ctx.Capability?.Name ?? ""
            ),
            new StaticVariable(
                "Capability.NameLink",
                "Display name as HTML link to capability",
                "Capability",
                "<a href=\"https://build.dfds.cloud/capabilities/my-capability-abc12\">My Capability</a>",
                ctx =>
                {
                    if (ctx.Capability == null)
                        return "";
                    var capId = ctx.Capability.Id.ToString();
                    var name = ctx.Capability.Name;
                    return $"<a href=\"https://{_portalBaseUrl}/capabilities/{capId}\">{name}</a>";
                }
            ),
            new StaticVariable(
                "Capability.Description",
                "Description text",
                "Capability",
                "A description...",
                ctx => ctx.Capability?.Description ?? ""
            ),
            new StaticVariable(
                "Capability.Status",
                "Active or Pending Deletion",
                "Capability",
                "Active",
                ctx => ctx.Capability?.Status.ToString() ?? ""
            ),
            new StaticVariable(
                "Capability.CreatedAt",
                "Creation date (yyyy-MM-dd)",
                "Capability",
                "2024-01-15",
                ctx => ctx.Capability?.CreatedAt.ToString("yyyy-MM-dd") ?? ""
            ),
            new StaticVariable(
                "Capability.CreatedBy",
                "Creator user ID",
                "Capability",
                "user@dfds.com",
                ctx => ctx.Capability?.CreatedBy ?? ""
            ),
            new StaticVariable(
                "Capability.RequirementScore",
                "Compliance score (0-100)",
                "Capability",
                "85",
                ctx => ctx.Capability?.RequirementScore?.ToString("0") ?? "N/A"
            ),
            new StaticVariable(
                "Capability.MemberCount",
                "Number of members",
                "Capability",
                "12",
                ctx => ctx.MemberCount.ToString()
            ),
            new StaticVariable(
                "Capability.Cost.7Days",
                "Total cost over the last 7 days (USD) with trend",
                "Capability",
                "$123.45 (-5.2%)",
                ctx => FormatCostWithTrend(ctx.Costs, 7)
            ),
            new StaticVariable(
                "Capability.Cost.14Days",
                "Total cost over the last 14 days (USD) with trend",
                "Capability",
                "$246.90 (-5.2%)",
                ctx => FormatCostWithTrend(ctx.Costs, 14)
            ),
            new StaticVariable(
                "Capability.Cost.30Days",
                "Total cost over the last 30 days (USD) with trend",
                "Capability",
                "$543.21 (-5.2%)",
                ctx => FormatCostWithTrend(ctx.Costs, 30)
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
            // --- Shared variables (both target types) ---
            new StaticVariable(
                "Campaign.Name",
                "Campaign name",
                "Campaign",
                "Q1 Migration Notice",
                ctx => ctx.CampaignName,
                Applies: AppliesTo.Both
            ),
            new StaticVariable(
                "Date.Today",
                "Current date (yyyy-MM-dd)",
                "Date",
                "2024-01-15",
                _ => DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Applies: AppliesTo.Both
            ),
            new StaticVariable(
                "Date.Year",
                "Current year",
                "Date",
                "2024",
                _ => DateTime.UtcNow.Year.ToString(),
                Applies: AppliesTo.Both
            ),
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
                (m, ctx) =>
                    ctx.AzureResources.FirstOrDefault(r => r.Environment == m.Groups["env"].Value)?.Id.ToString()
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
                (m, ctx) => LookupMetadata(ctx.Capability?.JsonMetadata, m.Groups["key"].Value),
                Hidden: true
            ),
            // --- User-targeted variables ---
            new StaticVariable(
                "User.Id",
                "Recipient user ID",
                "User",
                "user@dfds.com",
                ctx => ctx.Member?.Id.ToString() ?? "[User Id]",
                Applies: AppliesTo.User
            ),
            new StaticVariable(
                "User.Email",
                "Recipient email address",
                "User",
                "jane.doe@dfds.com",
                ctx => ctx.Member?.Email ?? "[User Email]",
                Applies: AppliesTo.User
            ),
            new StaticVariable(
                "User.DisplayName",
                "Recipient display name",
                "User",
                "Jane Doe",
                ctx => ctx.Member?.DisplayName ?? "[User Name]",
                Applies: AppliesTo.User
            ),
            new StaticVariable(
                "User.LastSeen",
                "Date the user was last seen in the portal (yyyy-MM-dd), or 'Never'",
                "User",
                "2024-01-15",
                ctx => ctx.Member?.LastSeen?.ToString("yyyy-MM-dd") ?? "Never",
                Applies: AppliesTo.User
            ),
            new StaticVariable(
                "User.CapabilityCount",
                "Number of capabilities the recipient belongs to",
                "User",
                "3",
                ctx => ctx.UserCapabilities.Count.ToString(),
                Applies: AppliesTo.User
            ),
            new StaticVariable(
                "User.CapabilityNames",
                "Comma-separated names of capabilities the recipient belongs to",
                "User",
                "Cap A, Cap B, Cap C",
                ctx => string.Join(", ", ctx.UserCapabilities.Select(uc => uc.Capability.Name)),
                Applies: AppliesTo.User
            ),
            // Documented as a block construct so users discover it via the variable picker.
            // Render-side handling lives in the {{#each User.Capabilities}} block pre-pass.
            new StaticVariable(
                "User.Capabilities",
                "Iterable: {{#each User.Capabilities}} ... {{Capability.Name}} ... {{/each}}",
                "User",
                "{{#each User.Capabilities}}...{{/each}}",
                _ => "",
                Applies: AppliesTo.User
            ),
        };
    }

    public string RenderTemplate(string template, TemplateRenderContext context)
    {
        var expanded = ExpandUserCapabilitiesBlocks(template, context);
        return TokenRegex.Replace(expanded, m => Resolve(m.Groups[1].Value, context) ?? m.Value);
    }

    public IReadOnlyList<TemplateVariable> GetVariableDefinitions(EmailCampaignTargetType? targetType = null)
    {
        var resolved = targetType ?? EmailCampaignTargetType.Capability;
        var isUser = resolved == EmailCampaignTargetType.User;

        return _variables
            .Where(v => !v.Hidden)
            // Capability campaigns: every variable that applies to a capability resolves directly.
            // User campaigns: top-level user/shared variables, plus all per-capability variables
            // (Capability.*, Requirement.*, Aws.*, Azure.*, MembershipApplications.*, Cost.*) — the
            // latter only resolve inside a {{#each User.Capabilities}} block, flagged via Scope.
            .Where(v =>
                isUser
                    ? v.Scope == VarScope.PerCapability || (v.Applies & AppliesTo.User) != 0
                    : (v.Applies & AppliesTo.Capability) != 0
            )
            .Select(v => new TemplateVariable
            {
                Name = v.Name,
                Description = v.Description,
                Entity = v.Entity,
                Example = v.Example,
                Scope = v.Scope == VarScope.PerCapability ? "perCapability" : "topLevel",
            })
            .ToList();
    }

    private string ExpandUserCapabilitiesBlocks(string template, TemplateRenderContext context)
    {
        return UserCapabilitiesBlockRegex.Replace(
            template,
            match =>
            {
                var body = match.Groups["body"].Value;
                if (context.UserCapabilities.Count == 0)
                    return "";

                var sb = new StringBuilder();
                foreach (var entry in context.UserCapabilities)
                {
                    // Inside the block, Capability.* resolves to this iteration's capability;
                    // Member, Campaign.Name, Date.* are inherited from the outer context. The
                    // per-capability data (AWS, Azure, requirement scores, pending applications)
                    // is carried on the entry — bulk-loaded once per campaign by the caller.
                    var subContext = context with
                    {
                        Capability = entry.Capability,
                        MemberCount = entry.MemberCount,
                        AwsAccount = entry.AwsAccount,
                        AzureResources = entry.AzureResources,
                        RequirementScores = entry.RequirementScores,
                        PendingMembershipApplicationCount = entry.PendingMembershipApplicationCount,
                        Costs = entry.Costs,
                    };
                    sb.Append(TokenRegex.Replace(body, m => Resolve(m.Groups[1].Value, subContext) ?? m.Value));
                }
                return sb.ToString();
            }
        );
    }

    private string? Resolve(string name, TemplateRenderContext context)
    {
        if (_byName.TryGetValue(name, out var def))
            return def.Resolve(context);

        foreach (var p in _patterns)
        {
            var match = p.Regex.Match(name);
            if (!match.Success)
                continue;
            return p.Resolve(match, context) ?? p.FallbackOnMiss;
        }

        return null;
    }

    // Matches the portal's cost display currency (USD); "N/A" when no cost data is cached.
    private static string FormatCost(float? value) => value is null ? "N/A" : "$" + value.Value.ToString("0.00");

    // Format cost with trend: "$543.21 (-5.2%)" or "$543.21 (no trend data)"
    private static string FormatCostWithTrend(CapabilityCosts? costs, int days)
    {
        if (costs?.Costs == null || costs.Costs.Length == 0)
            return "N/A";

        var costValue = costs.SumForLastDays(days);
        if (costValue == null)
            return "N/A";

        var trend = CalculateCostTrend(costs.Costs, days);
        var costStr = "$" + costValue.Value.ToString("0.00");
        
        if (trend == null)
            return costStr + " (no trend data)";
        
        return costStr + $" ({trend:0.0}%)";
    }

    // Calculate the trend percentage for a given period
    // Returns null if insufficient data, otherwise the percentage change
    private static decimal? CalculateCostTrend(TimeSeries[] costTimeSeries, int days)
    {
        if (costTimeSeries == null || costTimeSeries.Length == 0)
            return null;

        var now = DateTime.UtcNow;
        var currentStart = now.AddDays(-days);
        var previousStart = now.AddDays(-days * 2);

        // Current period: past {days} days
        var currentPeriod = costTimeSeries
            .Where(ts => ts.TimeStamp >= currentStart && ts.TimeStamp <= now)
            .ToArray();

        // Previous period: {days} days before that
        var previousPeriod = costTimeSeries
            .Where(ts => ts.TimeStamp >= previousStart && ts.TimeStamp < currentStart)
            .ToArray();

        // Need data in both periods
        if (currentPeriod.Length == 0 || previousPeriod.Length == 0)
            return null;

        var currentSum = (decimal)currentPeriod.Sum(ts => ts.Value);
        var previousSum = (decimal)previousPeriod.Sum(ts => ts.Value);

        // Both periods are zero: no change
        if (currentSum == 0 && previousSum == 0)
            return 0m;

        // Previous was zero but current is positive: undefined growth, return null
        if (previousSum == 0 && currentSum > 0)
            return null;

        // Calculate percentage change: ((current - previous) / previous) * 100
        return ((currentSum - previousSum) / previousSum) * 100;
    }

    private static string? ResolveRequirementScore(TemplateRenderContext ctx, string id)
    {
        // Tags are not stored in the requirements DB — they are derived from the capability's metadata,
        // matching how GET /compliance/capabilities/{id} computes the Tags score.
        if (id == TagComplianceEvaluator.RequirementId)
            return ctx.Capability is null
                ? null
                : TagComplianceEvaluator.Evaluate(ctx.Capability.JsonMetadata).Score.ToString("0");

        return ctx.RequirementScores.FirstOrDefault(s => s.RequirementId == id)?.Value.ToString("0");
    }

    private static string? ResolveRequirementDisplayName(TemplateRenderContext ctx, string id)
    {
        if (id == TagComplianceEvaluator.RequirementId)
            return ctx.Capability is null ? null : TagComplianceEvaluator.DisplayName;

        var metric = ctx.RequirementScores.FirstOrDefault(s => s.RequirementId == id);
        return metric is null ? null : (metric.DisplayName ?? metric.RequirementId);
    }

    private static string? ResolveRequirementHelpUrl(TemplateRenderContext ctx, string id)
    {
        if (id == TagComplianceEvaluator.RequirementId)
            return ctx.Capability is null ? null : TagComplianceEvaluator.HelpUrl;

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
