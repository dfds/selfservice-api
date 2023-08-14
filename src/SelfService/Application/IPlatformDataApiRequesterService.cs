using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IPlatformDataApiRequesterService
{
    Task<MyCapabilityCosts> GetMyCapabilitiesCosts(UserId userId, int daysWindow);
}
