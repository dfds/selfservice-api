using System.Text.Json;
using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.CopilotInsights;

// Undeclared upstream fields are dropped during deserialization. Keep net amounts,
// discount amounts, premium requests, and other addresses out of non-admin responses.

public sealed class CopilotEnvelope<T>
{
    public T? Data { get; set; }
    public CopilotMetaDto? Meta { get; set; }
}

public sealed class CopilotErrorDto
{
    public string? Error { get; set; }
    public string? Detail { get; set; }
}

public sealed class CopilotMetaDto
{
    public DateTime? GeneratedAt { get; set; }
    public Dictionary<string, DateTime?> Freshness { get; set; } = new();
    public string? MatchedEmail { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

public sealed class CopilotUserBundleDto
{
    public CopilotIdentityDto Identity { get; set; } = new();
    public CopilotBundleScopesDto Scopes { get; set; } = new();
    public List<CopilotSeatDto> Seats { get; set; } = new();
    public List<CopilotUserDayDto> Daily { get; set; } = new();
    public CopilotUserPeriodDto? Latest28d { get; set; }
    public CopilotUserCreditsDto? Credits { get; set; }
    public CopilotBudgetDto? Budget { get; set; }
}

/// <summary>
/// The AI credit budget covering the user in the current month, in USD. Consumed is GitHub's
/// figure, the gross amount of the month to date.
/// </summary>
public sealed class CopilotBudgetDto
{
    public decimal Amount { get; set; }
    public decimal Consumed { get; set; }
    public bool PreventFurtherUsage { get; set; }
}

public sealed class CopilotIdentityDto
{
    public long UserId { get; set; }
    public string Login { get; set; } = "";
    public string? Name { get; set; }
    public bool Seated { get; set; }
}

/// <summary>Search result retaining only the primary email.</summary>
public sealed class CopilotIdentitySummaryDto
{
    public string Login { get; set; } = "";
    public string? Name { get; set; }
    public string? PrimaryEmail { get; set; }
    public bool Seated { get; set; }
}

public sealed class CopilotBundleScopesDto
{
    public string? Seats { get; set; }
    public string? Reports { get; set; }
    public string? Credits { get; set; }
}

public sealed class CopilotSeatDto
{
    public string Scope { get; set; } = "";
    public string? OrgName { get; set; }
    public string? PlanType { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public string? LastActivityEditor { get; set; }
    public DateTime? LastAuthenticatedAt { get; set; }
    public DateOnly? PendingCancellationDate { get; set; }
}

/// <summary>One users-1-day report row. Counters absent upstream stay null.</summary>
public sealed class CopilotUserDayDto
{
    [JsonPropertyName("day")]
    public DateOnly Day { get; set; }

    [JsonPropertyName("user_initiated_interaction_count")]
    public long? UserInitiatedInteractionCount { get; set; }

    [JsonPropertyName("code_generation_activity_count")]
    public long? CodeGenerationActivityCount { get; set; }

    [JsonPropertyName("code_acceptance_activity_count")]
    public long? CodeAcceptanceActivityCount { get; set; }

    [JsonPropertyName("loc_suggested_to_add_sum")]
    public long? LocSuggestedToAddSum { get; set; }

    [JsonPropertyName("loc_suggested_to_delete_sum")]
    public long? LocSuggestedToDeleteSum { get; set; }

    [JsonPropertyName("loc_added_sum")]
    public long? LocAddedSum { get; set; }

    [JsonPropertyName("loc_deleted_sum")]
    public long? LocDeletedSum { get; set; }

    [JsonPropertyName("ai_credits_used")]
    public double? AiCreditsUsed { get; set; }

    [JsonPropertyName("used_agent")]
    public bool? UsedAgent { get; set; }

    [JsonPropertyName("used_chat")]
    public bool? UsedChat { get; set; }

    [JsonPropertyName("used_cli")]
    public bool? UsedCli { get; set; }

    [JsonPropertyName("used_copilot_coding_agent")]
    public bool? UsedCopilotCodingAgent { get; set; }

    [JsonPropertyName("used_copilot_cloud_agent")]
    public bool? UsedCopilotCloudAgent { get; set; }

    [JsonPropertyName("ides")]
    public List<string> Ides { get; set; } = new();

    [JsonPropertyName("languages")]
    public List<string> Languages { get; set; } = new();

    [JsonPropertyName("models")]
    public List<string> Models { get; set; } = new();
}

public sealed class CopilotUserPeriodDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public CopilotUserTotalsDto? Totals { get; set; }
    public Dictionary<string, List<CopilotFactDto>> Breakdowns { get; set; } = new();
}

public sealed class CopilotUserTotalsDto
{
    public int DaysActive { get; set; }
    public DateOnly? FirstDay { get; set; }
    public DateOnly? LastDay { get; set; }

    /// Keys use GitHub's snake_case field names; `used_*` flags are ORed.
    public Dictionary<string, JsonElement> Totals { get; set; } = new();
}

public sealed class CopilotFactDto
{
    public string Key1 { get; set; } = "";
    public string? Key2 { get; set; }
    public Dictionary<string, double> Metrics { get; set; } = new();
}

public sealed class CopilotUserCreditsDto
{
    public CopilotCreditTotalsDto? Totals { get; set; }
    public List<CopilotCreditDayDto> ByDay { get; set; } = new();
    public List<CopilotCreditModelDto> ByModel { get; set; } = new();
}

public sealed class CopilotCreditTotalsDto
{
    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("gross_amount")]
    public decimal GrossAmount { get; set; }
}

public sealed class CopilotCreditDayDto
{
    public DateOnly Day { get; set; }
    public CopilotCreditTotalsDto Totals { get; set; } = new();
}

public sealed class CopilotCreditModelDto
{
    public string Model { get; set; } = "";
    public CopilotCreditTotalsDto Totals { get; set; } = new();
}
