namespace SelfService.Domain.Models;

public interface IKubernetesAccessRepository
{
    Task Add(KubernetesAccess access);
    Task<List<KubernetesAccess>> GetAllBy(CapabilityId capabilityId);
    Task<List<KubernetesAccess>> GetAllBy(IEnumerable<CapabilityId> capabilityIds);
    Task<KubernetesAccess?> FindRequestedByAwsAccountId(AwsAccountId awsAccountId);
}
