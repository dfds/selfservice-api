namespace SelfService.Domain.Models;

public interface ICapabilityCalimRepository
{
    Task Add(CapabilityClaim claim);
}