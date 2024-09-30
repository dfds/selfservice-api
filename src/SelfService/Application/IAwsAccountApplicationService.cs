using SelfService.Domain.Events;
using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IAwsAccountApplicationService
{
    Task<AwsAccountId> RequestAwsAccount(CapabilityId capabilityId, UserId requestedBy);
    Task CreateAwsAccountRequestTicket(AwsAccountId id);
    Task RegisterRealAwsAccount(AwsAccountId id, RealAwsAccountId realAwsAccountId, string? roleEmail);
    Task LinkKubernetesNamespace(AwsAccountId id, string? @namespace);
    public Task PublishResourceManifestToGit(AwsAccountRequested awsAccountRequested);
}
