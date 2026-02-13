using System.Collections.Generic;
using System.Threading.Tasks;

namespace SelfService.Application
{
    public interface IRequirementScoreService
    {
        Task<(double totalScore, List<RequirementScore> scores)> GetRequirementScoreAsync(string capabilityId);
    }
}
