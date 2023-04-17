using Microsoft.AspNetCore.Authorization;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Authorization;

public class HasAwsAccount : IAuthorizationRequirement
{
    
}

public class HasAwsAccountHandler : AuthorizationHandler<HasAwsAccount, Capability>
{
    private readonly IAwsAccountRepository _awsAccountRepository;

    public HasAwsAccountHandler(IAwsAccountRepository awsAccountRepository)
    {
        _awsAccountRepository = awsAccountRepository;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, HasAwsAccount requirement, Capability resource)
    {
        if (await _awsAccountRepository.Exists(resource.Id))
        {
            context.Succeed(requirement);
        }
    }
}
