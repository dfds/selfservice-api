using Microsoft.AspNetCore.Authorization;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Authorization;

public class NotHasPendingMembershipApplication: IAuthorizationRequirement
{
    
}

public class NotHasPendingMembershipApplicationHandler : AuthorizationHandler<NotHasPendingMembershipApplication, Capability>
{
    private readonly IMembershipQuery _membershipQuery;

    public NotHasPendingMembershipApplicationHandler(IMembershipQuery membershipQuery)
    {
        _membershipQuery = membershipQuery;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        NotHasPendingMembershipApplication requirement, Capability resource)
    {
        if (!context.User.TryGetUserId(out var userId))
        {
            context.Fail(new AuthorizationFailureReason(this, $"Value \"{context.User?.Identity?.Name}\" is not a valid user id"));
            return;
        }

        if (!await _membershipQuery.HasActiveMembershipApplication(userId, resource.Id))
        {
            context.Succeed(requirement);
        }
    }
}