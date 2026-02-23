namespace SelfService.Domain.Models;

public interface ICapabilityRepository
{
    Task<Capability> Get(CapabilityId id);
    Task<Capability?> FindBy(CapabilityId id);
    Task<bool> Exists(CapabilityId id);
    Task Add(Capability capability);
    Task<IEnumerable<Capability>> GetAll();
    Task<IEnumerable<Capability>> GetAllPendingDeletionFor(int days);
    Task SetJsonMetadata(CapabilityId id, string jsonMetadata, int jsonSchemaVersion);
    Task<string> GetJsonMetadata(CapabilityId id);
    Task UpdateRequirementScore(CapabilityId id, double score);
}
