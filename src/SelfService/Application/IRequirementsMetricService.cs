using System.Collections.Generic;
using System.Threading.Tasks;

namespace SelfService.Application
{
    public interface IRequirementsMetricService
    {
        Task<(double totalScore, List<Infrastructure.Persistence.Models.RequirementsMetric> scores)> GetRequirementScoreAsync(string capabilityId);
    }
}
