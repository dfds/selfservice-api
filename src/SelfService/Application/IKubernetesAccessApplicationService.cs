using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IKubernetesAccessApplicationService
{
    Task RequestKubernetesAccess(AwsAccountId awsAccountId, UserId requestedBy);
    Task GrantKubernetesAccess(AwsAccountId awsAccountId, string namespaceName);
}
