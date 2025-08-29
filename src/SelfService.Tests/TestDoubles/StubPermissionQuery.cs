using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Tests.TestDoubles;

public class StubPermissionQuery : IPermissionQuery
{
    public StubPermissionQuery() { }

    public Task<IList<RbacPermissionGrant>> FindUserGroupPermissionsByUserId(string userId)
    {
        return Task.FromResult<IList<RbacPermissionGrant>>(new List<RbacPermissionGrant>());
    }

    public Task<IList<RbacRoleGrant>> FindUserGroupRolesByUserId(string userId)
    {
        return Task.FromResult<IList<RbacRoleGrant>>(new List<RbacRoleGrant>());
    }
}
