namespace SelfService.Domain.Models;

public interface IAzureResourceRepository
{
    Task<List<AzureResource>> GetFor(CapabilityId capabilityId);
    Task<List<AzureResource>> GetAll();
    Task Add(AzureResource azureResource);
    Task<AzureResource> Get(AzureResourceId id);
    Task<bool> Exists(CapabilityId capabilityId, string environment); // Todo: use enum for environment
    Task<bool> Any(CapabilityId capabilityId);
}
