using SelfService.Domain.Models;

namespace SelfService.Tests.TestDoubles;

public class StubRbacRoleGrantRepository : IRbacRoleGrantRepository
{
    public Task Add(RbacRoleGrant model)
    {
        return Task.CompletedTask;
    }

    public Task<bool> Exists(RbacRoleGrantId id)
    {
        return Task.FromResult(true);
    }

    public Task<RbacRoleGrant?> FindById(RbacRoleGrantId id)
    {
        return Task.FromResult<RbacRoleGrant?>(null);
    }

    public Task<RbacRoleGrant?> FindByPredicate(Func<RbacRoleGrant, bool> predicate)
    {
        return Task.FromResult<RbacRoleGrant?>(null);
    }

    public Task<RbacRoleGrant> Remove(RbacRoleGrantId id)
    {
        return Task.FromResult(
            new RbacRoleGrant(
                RbacRoleGrantId.New(),
                RbacRoleId.New(),
                DateTime.Now,
                AssignedEntityType.User,
                "",
                RbacAccessType.Capability,
                ""
            )
        );
    }

    public Task<List<RbacRoleGrant>> GetAll()
    {
        return Task.FromResult(new List<RbacRoleGrant>());
    }

    public Task<List<RbacRoleGrant>> GetAllWithPredicate(Func<RbacRoleGrant, bool> predicate)
    {
        return Task.FromResult(new List<RbacRoleGrant>());
    }
}
