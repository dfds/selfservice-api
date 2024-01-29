using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IConfigurationLevelService
{
    public Task<ConfigurationLevelInfo> ComputeConfigurationLevel(CapabilityId capabilityId);
}
