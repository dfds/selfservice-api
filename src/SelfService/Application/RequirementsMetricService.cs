using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SelfService.Application
{
    // Consolidated: Use RequirementsMetric from Infrastructure.Persistence.Models

    public class RequirementsMetricService : IRequirementsMetricService
    {
        private readonly Infrastructure.Persistence.RequirementsDbContext _requirementsDbContext;

        public RequirementsMetricService(Infrastructure.Persistence.RequirementsDbContext requirementsDbContext)
        {
            _requirementsDbContext = requirementsDbContext;
        }

        public async Task<(double totalScore, List<Infrastructure.Persistence.Models.RequirementsMetric> scores)> GetRequirementScoreAsync(string capabilityId)
        {
            var scores = await _requirementsDbContext.Metrics
                .Where(x => x.CapabilityRootId == capabilityId)
                .Where(x => x.Measurement == "score")
                .ToListAsync();
            double totalScore = scores.Count > 0 ? scores.Average(r => r.Value) : 100;
            return (totalScore, scores);
        }
    }
}
