using Microsoft.AspNetCore.Authorization;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Authorization;

public class CapabilityHasMultipleMembers : IAuthorizationRequirement
{

}

public class CapabilityHasMultipleMembersHandler : AuthorizationHandler<CapabilityHasMultipleMembers, Capability>
{
    private readonly IMembershipQuery _membershipQuery;

    public CapabilityHasMultipleMembersHandler(IMembershipQuery membershipQuery)
    {
        _membershipQuery = membershipQuery;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CapabilityHasMultipleMembers requirement, Capability resource)
    {
        if (await _membershipQuery.HasMultipleMembers(resource.Id))
        {
            context.Succeed(requirement);
        }
    }
}