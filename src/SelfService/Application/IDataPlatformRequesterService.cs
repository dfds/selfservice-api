using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IDataPlatformRequesterService
{
    Task<CapabilityCosts> GetCapabilityCosts(CapabilityId capabilityId, int daysWindow);
}
