using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public interface ICapabilityXaxaRepository
{
    public Task Add(CapabilityXaxa capabilityXaxa);
}