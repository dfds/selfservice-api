namespace SelfService.Domain.Models;

public interface IAwsAccountRepository
{
    Task<AwsAccount?> FindBy(CapabilityId capabilityId);
    Task<List<AwsAccount>> GetAll();
    Task<List<AwsAccount>> GetByCapabilityIds(IEnumerable<CapabilityId> capabilityIds);
    Task<AwsAccount> Get(AwsAccountId id);
    Task Add(AwsAccount account);
    Task<bool> Exists(CapabilityId capabilityId);
}
