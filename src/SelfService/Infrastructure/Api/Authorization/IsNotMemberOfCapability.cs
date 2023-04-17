using Microsoft.AspNetCore.Authorization;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Authorization;

public class IsNotMemberOfCapability : IAuthorizationRequirement
{
    
}

public class IsNotMemberOfCapabilityHandler : AuthorizationHandler<IsNotMemberOfCapability, Capability>
{
    private readonly IMembershipQuery _membershipQuery;

    public IsNotMemberOfCapabilityHandler(IMembershipQuery membershipQuery)
    {
        _membershipQuery = membershipQuery;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, IsNotMemberOfCapability requirement, Capability resource)
    {
        if (!context.User.TryGetUserId(out var userId))
        {
            context.Fail(new AuthorizationFailureReason(this, $"Value \"{context.User?.Identity?.Name}\" is not a valid user id"));
            return;
        }

        if (!await _membershipQuery.HasActiveMembership(userId, resource.Id))
        {
            context.Succeed(requirement);
        }
    }
}