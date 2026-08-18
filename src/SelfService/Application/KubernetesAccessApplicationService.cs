using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Application;

public class KubernetesAccessApplicationService : IKubernetesAccessApplicationService
{
    private readonly IAwsAccountRepository _awsAccountRepository;
    private readonly IKubernetesAccessRepository _kubernetesAccessRepository;
    private readonly SystemTime _systemTime;

    public KubernetesAccessApplicationService(
        IAwsAccountRepository awsAccountRepository,
        IKubernetesAccessRepository kubernetesAccessRepository,
        SystemTime systemTime
    )
    {
        _awsAccountRepository = awsAccountRepository;
        _kubernetesAccessRepository = kubernetesAccessRepository;
        _systemTime = systemTime;
    }

    [TransactionalBoundary, Outboxed]
    public async Task RequestKubernetesAccess(AwsAccountId awsAccountId, UserId requestedBy)
    {
        var account = await _awsAccountRepository.Get(awsAccountId);

        if (account.Status != AwsAccountStatus.Completed)
        {
            throw new InvalidOperationException(
                $"AWS account must be completed before requesting Kubernetes access. Current status: {account.Status}"
            );
        }

        var access = KubernetesAccess.Request(
            capabilityId: account.CapabilityId,
            environment: account.Environment,
            awsAccountId: awsAccountId,
            requestedAt: _systemTime.Now,
            requestedBy: requestedBy
        );

        await _kubernetesAccessRepository.Add(access);
    }

    [TransactionalBoundary, Outboxed]
    public async Task GrantKubernetesAccess(AwsAccountId awsAccountId, string namespaceName)
    {
        var access = await _kubernetesAccessRepository.FindRequestedByAwsAccountId(awsAccountId);
        if (access is null)
        {
            throw new InvalidOperationException(
                $"No pending Kubernetes access request found for AWS account {awsAccountId}"
            );
        }

        access.GrantAccess(namespaceName, _systemTime.Now);
    }
}
