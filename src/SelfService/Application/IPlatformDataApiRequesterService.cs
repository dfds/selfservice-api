using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IPlatformDataApiRequesterService
{
    Task<MyCapabilityCosts> GetMyCapabilityCosts(UserId userId, int daysWindow);
}