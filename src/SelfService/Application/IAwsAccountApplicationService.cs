using SelfService.Domain.Events;
using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IAwsAccountApplicationService
{
    Task<AwsAccountId> RequestAwsAccount(CapabilityId capabilityId, string environment, UserId requestedBy);
    Task RegisterRealAwsAccount(AwsAccountId id, RealAwsAccountId realAwsAccountId, string? roleEmail);
    public Task PublishResourceManifestToGit(AwsAccountRequested awsAccountRequested);
}
