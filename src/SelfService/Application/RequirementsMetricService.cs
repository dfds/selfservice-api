using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Application
{
    // Consolidated: Use RequirementsMetric from Infrastructure.Persistence.Models

    public class RequirementsMetricService : IRequirementsMetricService
    {
        private readonly Infrastructure.Persistence.RequirementsDbContext _requirementsDbContext;

        public RequirementsMetricService(
            Infrastructure.Persistence.RequirementsDbContext requirementsDbContext,
            ICapabilityRepository capabilityRepository
        )
        {
            _requirementsDbContext = requirementsDbContext;
        }

        public async Task<(
            double totalScore,
            List<Infrastructure.Persistence.Models.RequirementsMetric> scores
        )> GetRequirementScoreAsync(string capabilityId)
        {
            var scores = await _requirementsDbContext
                .Metrics.Where(x => x.CapabilityRootId == capabilityId)
                .Where(x => x.Measurement == "score")
                .ToListAsync();
            double totalScore = scores.Count > 0 ? scores.Average(r => r.Value) : 100;
            return (totalScore, scores);
        }

        public async Task<Dictionary<string, double>> GetAllRequirementScoresAsync()
        {
            var metrics = await _requirementsDbContext.Metrics.Where(x => x.Measurement == "score").ToListAsync();

            return metrics.GroupBy(m => m.CapabilityRootId).ToDictionary(g => g.Key, g => g.Average(m => m.Value));
        }
    }

    // Stub implementation for when RequirementsDbContext is not available
    public class StubRequirementsMetricService : IRequirementsMetricService
    {
        public Task<(
            double totalScore,
            List<Infrastructure.Persistence.Models.RequirementsMetric> scores
        )> GetRequirementScoreAsync(string capabilityId)
        {
            // Return default values: perfect score with no metrics
            return Task.FromResult((100.0, new List<Infrastructure.Persistence.Models.RequirementsMetric>()));
        }

        public Task<Dictionary<string, double>> GetAllRequirementScoresAsync() =>
            Task.FromResult(new Dictionary<string, double>());
    }
}
