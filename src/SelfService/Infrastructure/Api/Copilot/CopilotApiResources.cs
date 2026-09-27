using System.Text.Json;
using System.Text.Json.Serialization;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Copilot;

public class CopilotMetaApiResource
{
    public bool CopilotAvailable { get; init; }
    public DateTime? GeneratedAt { get; init; }

    /// Last successful run per copilot-insights collector; null when it has never succeeded.
    public IReadOnlyDictionary<string, DateTime?> Freshness { get; init; } = new Dictionary<string, DateTime?>();
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }

    public string? MatchedEmail { get; init; }
}

public class CopilotSeatApiResource
{
    public string Scope { get; init; } = "";
    public string? OrgName { get; init; }
    public string? PlanType { get; init; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? LastActivityAt { get; init; }
    public string? LastActivityEditor { get; init; }
    public DateTime? LastAuthenticatedAt { get; init; }
    public DateOnly? PendingCancellationDate { get; init; }
}

public class CopilotDayApiResource
{
    public DateOnly Day { get; init; }
    public long? Interactions { get; init; }
    public long? CodeGenerations { get; init; }
    public long? CodeAcceptances { get; init; }
    public long? LocSuggestedToAdd { get; init; }
    public long? LocSuggestedToDelete { get; init; }
    public long? LocAdded { get; init; }
    public long? LocDeleted { get; init; }
    public double? AiCreditsUsed { get; init; }
    public bool? UsedAgent { get; init; }
    public bool? UsedChat { get; init; }
    public bool? UsedCli { get; init; }
    public bool? UsedCodingAgent { get; init; }
    public bool? UsedCloudAgent { get; init; }
    public IReadOnlyList<string> Ides { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Languages { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Models { get; init; } = Array.Empty<string>();
}

public class CopilotFactApiResource
{
    public string Key1 { get; init; } = "";
    public string? Key2 { get; init; }

    /// Keys use GitHub's snake_case metric names.
    public IReadOnlyDictionary<string, double> Metrics { get; init; } = new Dictionary<string, double>();
}

public class CopilotPeriodApiResource
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public int DaysActive { get; init; }
    public DateOnly? FirstDay { get; init; }
    public DateOnly? LastDay { get; init; }

    /// Keys use GitHub's snake_case field names; `used_*` flags are ORed.
    public IReadOnlyDictionary<string, JsonElement> Totals { get; init; } = new Dictionary<string, JsonElement>();

    /// Keys: ide, feature, language_model, model_feature, cli.
    public IReadOnlyDictionary<string, IReadOnlyList<CopilotFactApiResource>> Breakdowns { get; init; } =
        new Dictionary<string, IReadOnlyList<CopilotFactApiResource>>();
}

/// AI credits and their list price in USD. The included credits are pooled across the
/// enterprise and used up in the first days of the month, so a user's net amount depends on
/// when they used Copilot; net and discount amounts are admin-only.
public class CopilotCreditAmountApiResource
{
    public decimal Quantity { get; init; }
    public decimal GrossAmount { get; init; }
}

public class CopilotCreditDayApiResource : CopilotCreditAmountApiResource
{
    public DateOnly Day { get; init; }
}

public class CopilotCreditModelApiResource : CopilotCreditAmountApiResource
{
    public string Model { get; init; } = "";
}

public class CopilotCreditsApiResource
{
    public CopilotCreditAmountApiResource Totals { get; init; } = new();
    public IReadOnlyList<CopilotCreditDayApiResource> ByDay { get; init; } = Array.Empty<CopilotCreditDayApiResource>();
    public IReadOnlyList<CopilotCreditModelApiResource> ByModel { get; init; } =
        Array.Empty<CopilotCreditModelApiResource>();
}

/// The AI credit budget covering the user this month, in USD. Consumed is GitHub's figure, the
/// gross amount of the month's credits.
public class CopilotBudgetApiResource
{
    public decimal Amount { get; init; }
    public decimal Consumed { get; init; }

    public bool PreventFurtherUsage { get; init; }
}

public class CopilotMeLinks
{
    public ResourceLink? Self { get; init; }
    public ResourceLink? Credits { get; init; }

    /// User search, present on <c>/copilot/me</c> only when the caller holds copilot/read-all.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ResourceLink? Users { get; init; }
}

/// Returned by <c>/copilot/me</c> for the caller and by <c>/copilot/users/{login}</c> for a login.
public class CopilotMeApiResource
{
    /// False when no GitHub login is linked to the caller, or the login is unknown; see
    /// <see cref="UnmappedReason"/>.
    public bool Mapped { get; init; }

    /// service_principal, no_dfds_email, not_mapped or user_not_found.
    public string? UnmappedReason { get; init; }
    public string? Login { get; init; }
    public string? Name { get; init; }
    public bool Seated { get; init; }
    public IReadOnlyList<CopilotSeatApiResource> Seats { get; init; } = Array.Empty<CopilotSeatApiResource>();
    public IReadOnlyList<CopilotDayApiResource> Daily { get; init; } = Array.Empty<CopilotDayApiResource>();
    public CopilotPeriodApiResource? Latest28d { get; init; }
    public CopilotCreditsApiResource? Credits { get; init; }

    /// Covers the current month whatever the range; null when no budget covers the user.
    public CopilotBudgetApiResource? Budget { get; init; }
    public CopilotMetaApiResource Meta { get; init; } = new();

    [JsonPropertyName("_links")]
    public CopilotMeLinks Links { get; init; } = new();
}

public class CopilotUserSummaryApiResource
{
    public string Login { get; init; } = "";
    public string? Name { get; init; }
    public string? PrimaryEmail { get; init; }
    public bool Seated { get; init; }
}

public class CopilotUserSearchMetaApiResource
{
    public bool CopilotAvailable { get; init; }
}

public class CopilotUserSearchApiResource
{
    public IReadOnlyList<CopilotUserSummaryApiResource> Items { get; init; } =
        Array.Empty<CopilotUserSummaryApiResource>();
    public CopilotUserSearchMetaApiResource Meta { get; init; } = new();
}

public class CopilotMeCreditsApiResource
{
    public bool Mapped { get; init; }
    public string? UnmappedReason { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public CopilotCreditsApiResource? Credits { get; init; }
    public CopilotMetaApiResource Meta { get; init; } = new();

    [JsonPropertyName("_links")]
    public CopilotMeLinks Links { get; init; } = new();
}
