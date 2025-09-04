using SelfService.Domain.Models;

namespace SelfService.Tests.TestDoubles;

public class StubRbacPermissionGrantRepository : IRbacPermissionGrantRepository
{
    private readonly RbacPermissionGrant[] _permissions;

    public StubRbacPermissionGrantRepository(params RbacPermissionGrant[] permissions)
    {
        _permissions = permissions;
    }

    public Task Add(RbacPermissionGrant model)
    {
        return Task.CompletedTask;
    }

    public Task<bool> Exists(RbacPermissionGrantId id)
    {
        return Task.FromResult(_permissions.Any(x => x.Id == id));
    }

    public Task<RbacPermissionGrant?> FindById(RbacPermissionGrantId id)
    {
        return Task.FromResult<RbacPermissionGrant?>(_permissions.FirstOrDefault(x => x.Id == id));
    }

    public Task<RbacPermissionGrant?> FindByPredicate(Func<RbacPermissionGrant, bool> predicate)
    {
        return Task.FromResult<RbacPermissionGrant?>(_permissions.FirstOrDefault(predicate));
    }

    public Task<RbacPermissionGrant> Remove(RbacPermissionGrantId id)
    {
        // Fluttershy: should we maintain some state for this stub?
        return Task.FromResult(_permissions.First(x => x.Id == id));
    }

    public Task<List<RbacPermissionGrant>> GetAll()
    {
        return Task.FromResult(new List<RbacPermissionGrant>(_permissions));
    }

    public Task<List<RbacPermissionGrant>> GetAllWithPredicate(Func<RbacPermissionGrant, bool> predicate)
    {
        return Task.FromResult(new List<RbacPermissionGrant>(_permissions.Where(predicate)));
    }
}
