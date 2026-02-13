using System.Collections.Generic;
using Json.Schema;
using SelfService.Application;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Capabilities
{
    public static class RequirementScoreConverter
    {
        public static RequirementScoreApiResource Convert(
            string capabilityId,
            double totalScore,
            List<RequirementScore> scores
        )
        {
            var requirementScores = new List<RequirementScoreDetail>();
            foreach (var score in scores)
            {
                requirementScores.Add(
                    new RequirementScoreDetail
                    {
                        RequirementId = score.RequirementId,
                        Title = score.Title,
                        Score = score.Score,
                        Description = score.Description,
                    }
                );
            }
            return new RequirementScoreApiResource
            {
                CapabilityId = capabilityId,
                TotalScore = totalScore,
                RequirementScores = requirementScores,
            };
        }
    }
}
