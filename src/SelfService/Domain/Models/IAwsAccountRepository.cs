namespace SelfService.Domain.Models;

public interface IAwsAccountRepository    
{
    Task<AwsAccount?> FindBy(CapabilityId capabilityId);
    Task<List<AwsAccount>> GetAll();
}