using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IPlatformDataApiRequesterService
{
    Task<CapabilityCosts> GetCapabilityCosts(CapabilityId capabilityId, int daysWindow);
    Task<List<CapabilityCosts>> GetAllCapabilityCosts(int daysWindow);
}
