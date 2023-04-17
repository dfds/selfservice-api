using Microsoft.AspNetCore.Authorization;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Authorization;

public class HasPendingMembershipApplication: IAuthorizationRequirement
{
    
}

public class HasPendingMembershipApplicationHandler : AuthorizationHandler<HasPendingMembershipApplication, Capability>
{
    private readonly IMembershipQuery _membershipQuery;

    public HasPendingMembershipApplicationHandler(IMembershipQuery membershipQuery)
    {
        _membershipQuery = membershipQuery;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        HasPendingMembershipApplication requirement, Capability resource)
    {
        if (!context.User.TryGetUserId(out var userId))
        {
            context.Fail(new AuthorizationFailureReason(this, $"Value \"{context.User?.Identity?.Name}\" is not a valid user id"));
            return;
        }

        if (await _membershipQuery.HasActiveMembershipApplication(userId, resource.Id))
        {
            context.Succeed(requirement);
        }
    }
}