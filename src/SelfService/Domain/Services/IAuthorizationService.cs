using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Kafka;

namespace SelfService.Domain.Services;

public interface IAuthorizationService
{
    Task<UserAccessLevelOptions> GetUserAccessLevelForCapability(UserId userId, CapabilityId capabilityId);
}