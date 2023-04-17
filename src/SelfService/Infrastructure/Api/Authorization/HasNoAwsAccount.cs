using Microsoft.AspNetCore.Authorization;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Authorization;

public class HasNoAwsAccount : IAuthorizationRequirement
{
    
}

public class HasNoAwsAccountHandler : AuthorizationHandler<HasNoAwsAccount, Capability>
{
    private readonly IAwsAccountRepository _awsAccountRepository;

    public HasNoAwsAccountHandler(IAwsAccountRepository awsAccountRepository)
    {
        _awsAccountRepository = awsAccountRepository;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, HasNoAwsAccount requirement, Capability resource)
    {
        if (!await _awsAccountRepository.Exists(resource.Id))
        {
            context.Succeed(requirement);
        }
    }
}
