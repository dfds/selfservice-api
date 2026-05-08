using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.TestDoubles;

public class StubRbacRoleRepository : IRbacRoleRepository
{
    private readonly List<RbacRole> _roles;

    public StubRbacRoleRepository(params RbacRole[] roles)
    {
        _roles = roles.ToList();
    }

    public Task Add(RbacRole model)
    {
        _roles.Add(model);
        return Task.CompletedTask;
    }

    public Task<bool> Exists(RbacRoleId id)
    {
        return Task.FromResult(_roles.Any(r => r.Id == id));
    }

    public Task<RbacRole?> FindById(RbacRoleId id)
    {
        return Task.FromResult(_roles.FirstOrDefault(r => r.Id == id));
    }

    public Task<RbacRole?> FindByPredicate(Func<RbacRole, bool> predicate)
    {
        return Task.FromResult(_roles.FirstOrDefault(predicate));
    }

    public Task<RbacRole> Remove(RbacRoleId id)
    {
        var role = _roles.First(r => r.Id == id);
        _roles.Remove(role);
        return Task.FromResult(role);
    }

    public Task<List<RbacRole>> GetAll()
    {
        return Task.FromResult(_roles.ToList());
    }

    public Task<List<RbacRole>> GetAllWithPredicate(Func<RbacRole, bool> predicate)
    {
        return Task.FromResult(_roles.Where(predicate).ToList());
    }
}
