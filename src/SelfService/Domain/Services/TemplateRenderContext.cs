using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence.Models;

namespace SelfService.Domain.Services;

public record TemplateRenderContext
{
    public required Capability Capability { get; init; }
    public Member? Member { get; init; }
    public required string CampaignName { get; init; }
    public int MemberCount { get; init; }
    public AwsAccount? AwsAccount { get; init; }
    public List<AzureResource> AzureResources { get; init; } = new();
    public List<RequirementsMetric> RequirementScores { get; init; } = new();
    public int PendingMembershipApplicationCount { get; init; }
}
