namespace SelfService.Domain.Models;

public interface ICapabilityRepository
{
    Task<Capability> Get(CapabilityId id);
    Task<bool> Exists(CapabilityId id);
    Task Add(Capability capability);
}