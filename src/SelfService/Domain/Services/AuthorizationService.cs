using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Domain.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly ILogger<AuthorizationService> _logger;
    private readonly IMembershipQuery _membershipQuery;

    public AuthorizationService(ILogger<AuthorizationService> logger, IMembershipQuery membershipQuery)
    {
        _logger = logger;
        _membershipQuery = membershipQuery;
    }

    public async Task<UserAccessLevelOptions> GetUserAccessLevelForCapability(UserId userId, CapabilityId capabilityId)
    {
        var hasActiveMembership = await _membershipQuery.HasActiveMembership(userId, capabilityId);
        
        return hasActiveMembership
            ? UserAccessLevelOptions.ReadWrite
            : UserAccessLevelOptions.Read;
    }
}