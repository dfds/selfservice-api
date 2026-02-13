using System.Collections.Generic;

namespace SelfService.Infrastructure.Api.Capabilities
{
    public class RequirementScoreApiResource
    {
        public required string CapabilityId { get; set; }
        public double TotalScore { get; set; }
        public required List<RequirementScoreDetail> RequirementScores { get; set; }
    }

    public class RequirementScoreDetail
    {
        public required string RequirementId { get; set; }
        public required string Title { get; set; }
        public double Score { get; set; }
        public required string Description { get; set; }
    }
}
