using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Application;

public class AwsAccountApplicationService : IAwsAccountApplicationService
{
    private readonly IAwsAccountRepository _awsAccountRepository;
    private readonly SystemTime _systemTime;

    public AwsAccountApplicationService(IAwsAccountRepository awsAccountRepository, SystemTime systemTime)
    {
        _awsAccountRepository = awsAccountRepository;
        _systemTime = systemTime;
    }

    [TransactionalBoundary, Outboxed]
    public async Task<AwsAccountId> RequestAwsAccount(CapabilityId capabilityId, UserId requestedBy)
    {
        if (await _awsAccountRepository.Exists(capabilityId))
        {
            throw new AlreadyHasAwsAccountException($"Capability {capabilityId} already has an AWS account");
        }

        var account = AwsAccount.RequestNew(capabilityId, _systemTime.Now, requestedBy);

        await _awsAccountRepository.Add(account);

        return account.Id;
    }
}