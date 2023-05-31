using Microsoft.AspNetCore.Authorization;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Authorization;

[Obsolete]
public class IsMemberOfCapability : IAuthorizationRequirement
{

}

public class IsMemberOfCapabilityByIdHandler : AuthorizationHandler<IsMemberOfCapability, CapabilityId>
{
    private readonly IMembershipQuery _membershipQuery;

    public IsMemberOfCapabilityByIdHandler(IMembershipQuery membershipQuery)
    {
        _membershipQuery = membershipQuery;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, IsMemberOfCapability requirement, CapabilityId resource)
    {
        if (!context.User.TryGetUserId(out var userId))
        {
            context.Fail(new AuthorizationFailureReason(this, $"Value \"{context.User?.Identity?.Name}\" is not a valid user id"));
            return;
        }

        if (await _membershipQuery.HasActiveMembership(userId, resource))
        {
            context.Succeed(requirement);
        }
    }
}

public class IsMemberOfCapabilityHandler : AuthorizationHandler<IsMemberOfCapability, Capability>
{
    private readonly IMembershipQuery _membershipQuery;

    public IsMemberOfCapabilityHandler(IMembershipQuery membershipQuery)
    {
        _membershipQuery = membershipQuery;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, IsMemberOfCapability requirement, Capability resource)
    {
        if (!context.User.TryGetUserId(out var userId))
        {
            context.Fail(new AuthorizationFailureReason(this, $"Value \"{context.User?.Identity?.Name}\" is not a valid user id"));
            return;
        }

        if (await _membershipQuery.HasActiveMembership(userId, resource.Id))
        {
            context.Succeed(requirement);
        }
    }
}