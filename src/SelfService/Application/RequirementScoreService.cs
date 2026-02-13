using System.Collections.Generic;
using System.Threading.Tasks;

namespace SelfService.Application
{
    public class RequirementScore
    {
        public required string RequirementId { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public double Score { get; set; }
    }

    public class RequirementScoreService : IRequirementScoreService
    {
        public Task<(double totalScore, List<RequirementScore> scores)> GetRequirementScoreAsync(string capabilityId)
        {
            var scores = new List<RequirementScore>
            {
                new RequirementScore
                {
                    RequirementId = "REQ-1",
                    Title = "Security",
                    Description = "Security controls are in place.",
                    Score = 85,
                },
                new RequirementScore
                {
                    RequirementId = "REQ-2",
                    Title = "Compliance",
                    Description = "Compliance requirements met.",
                    Score = 70,
                },
                new RequirementScore
                {
                    RequirementId = "REQ-3",
                    Title = "Performance",
                    Description = "Performance is optimal.",
                    Score = 95,
                },
                new RequirementScore
                {
                    RequirementId = "REQ-4",
                    Title = "Reliability",
                    Description = "System reliability is ensured.",
                    Score = 60,
                },
                new RequirementScore
                {
                    RequirementId = "REQ-5",
                    Title = "Usability",
                    Description = "Usability standards are met.",
                    Score = 78,
                },
                new RequirementScore
                {
                    RequirementId = "REQ-6",
                    Title = "Maintainability",
                    Description = "Maintainability is good.",
                    Score = 88,
                },
                new RequirementScore
                {
                    RequirementId = "REQ-7",
                    Title = "Scalability",
                    Description = "Scalability requirements fulfilled.",
                    Score = 92,
                },
                new RequirementScore
                {
                    RequirementId = "REQ-7",
                    Title = "Snyk",
                    Description = "Snyk should be used to scan the code.",
                    Score = 20,
                },
            };
            double totalScore = scores.Count > 0 ? scores.Average(r => r.Score) : 0;
            return Task.FromResult((totalScore, scores));
        }
    }
}
