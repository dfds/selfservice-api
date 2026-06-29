using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence.Models;

namespace SelfService.Domain.Services;

public record TemplateRenderContext
{
    /// <summary>
    /// Required for Capability-targeted campaigns. Null at the top level of a User-targeted
    /// campaign (where the body is rooted at a User, not a Capability); a non-null Capability
    /// is supplied inside each iteration of a {{#each User.Capabilities}} block.
    /// </summary>
    public Capability? Capability { get; init; }
    public Member? Member { get; init; }
    public required string CampaignName { get; init; }
    public int MemberCount { get; init; }
    public AwsAccount? AwsAccount { get; init; }
    public List<AzureResource> AzureResources { get; init; } = new();
    public List<RequirementsMetric> RequirementScores { get; init; } = new();
    public int PendingMembershipApplicationCount { get; init; }

    /// <summary>
    /// Cached cost time series for <see cref="Capability"/>, used by the Capability.Cost.* variables.
    /// Null when no cost data is available for the capability.
    /// </summary>
    public CapabilityCosts? Costs { get; init; }

    /// <summary>
    /// Capabilities the recipient user belongs to. Used for User-targeted campaigns,
    /// in particular by the {{#each User.Capabilities}} template block.
    /// </summary>
    public List<UserCapabilityRef> UserCapabilities { get; init; } = new();
}

public record UserCapabilityRef
{
    public required Capability Capability { get; init; }
    public int MemberCount { get; init; }
    public AwsAccount? AwsAccount { get; init; }
    public List<AzureResource> AzureResources { get; init; } = new();
    public List<RequirementsMetric> RequirementScores { get; init; } = new();
    public int PendingMembershipApplicationCount { get; init; }
    public CapabilityCosts? Costs { get; init; }
}
