using SelfService.Domain.Models;

namespace SelfService.Tests.TestDoubles;

public class StubRbacPermissionGrantRepository : IRbacPermissionGrantRepository
{
    public Task Add(RbacPermissionGrant model)
    {
        return Task.CompletedTask;
    }

    public Task<bool> Exists(RbacPermissionGrantId id)
    {
        return Task.FromResult(true);
    }

    public Task<RbacPermissionGrant?> FindById(RbacPermissionGrantId id)
    {
        return Task.FromResult<RbacPermissionGrant?>(null);
    }

    public Task<RbacPermissionGrant?> FindByPredicate(Func<RbacPermissionGrant, bool> predicate)
    {
        return Task.FromResult<RbacPermissionGrant?>(null);
    }

    public Task<RbacPermissionGrant> Remove(RbacPermissionGrantId id)
    {
        return Task.FromResult(
            new RbacPermissionGrant(
                RbacPermissionGrantId.New(),
                DateTime.Now,
                AssignedEntityType.User,
                "",
                "",
                "",
                "",
                ""
            )
        );
    }

    public Task<List<RbacPermissionGrant>> GetAll()
    {
        return Task.FromResult(new List<RbacPermissionGrant>());
    }

    public Task<List<RbacPermissionGrant>> GetAllWithPredicate(Func<RbacPermissionGrant, bool> predicate)
    {
        return Task.FromResult(new List<RbacPermissionGrant>());
    }
}
