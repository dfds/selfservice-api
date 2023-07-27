using SelfService.Domain.Models;

namespace SelfService.Tests.TestDoubles;

public class StubCapabilityRepository : ICapabilityRepository
{
    private readonly Capability? _capability;

    public StubCapabilityRepository(Capability? capability = null)
    {
        _capability = capability;
    }

    public Task<Capability> Get(CapabilityId id)
    {
        return Task.FromResult(_capability!);
    }

    public Task<Capability?> FindBy(CapabilityId id)
    {
        return Task.FromResult(_capability);
    }

    public Task<bool> Exists(CapabilityId id)
    {
        return Task.FromResult(_capability != null);
    }

    public Task Add(Capability capability)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Capability>> GetAll()
    {
        throw new NotImplementedException();
    }
}