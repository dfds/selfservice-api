using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

[Obsolete]
public interface IAuthorizationService
{
    [Obsolete]
    Task<UserAccessLevelOptions> GetUserAccessLevelForCapability(UserId userId, CapabilityId capabilityId);
}