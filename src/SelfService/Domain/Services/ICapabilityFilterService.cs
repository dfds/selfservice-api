using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public interface ICapabilityFilterService
{
    Task<List<Capability>> ResolveCapabilities(string audienceJson);
}
