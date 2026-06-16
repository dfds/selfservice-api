using System.Collections.Generic;
using System.Threading.Tasks;

namespace SelfService.Application
{
    public interface IRequirementsMetricService
    {
        Task<(
            double totalScore,
            List<Infrastructure.Persistence.Models.RequirementsMetric> scores
        )> GetRequirementScoreAsync(string capabilityId);

        Task<Dictionary<string, double>> GetAllRequirementScoresAsync();

        Task<
            Dictionary<string, List<Infrastructure.Persistence.Models.RequirementsMetric>>
        > GetRequirementScoresForCapabilitiesAsync(IReadOnlyCollection<string> capabilityIds);
    }
}
