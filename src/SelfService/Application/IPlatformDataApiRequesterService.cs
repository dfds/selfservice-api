using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IPlatformDataApiRequesterService
{
    Task<MyCapabilitiesMetrics> GetMyCapabilitiesMetrics(UserId userId);
}
