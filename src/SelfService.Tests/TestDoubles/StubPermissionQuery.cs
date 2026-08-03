using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Tests.TestDoubles;

public class StubPermissionQuery : IPermissionQuery
{
    private readonly List<RbacPermissionGrant> _guestPermissions;

    // Defaults to an empty guest baseline so existing tests see no implicit permissions.
    public StubPermissionQuery(List<RbacPermissionGrant>? guestPermissions = null)
    {
        _guestPermissions = guestPermissions ?? new List<RbacPermissionGrant>();
    }

    public Task<IList<RbacPermissionGrant>> FindUserGroupPermissionsByUserId(string userId)
    {
        return Task.FromResult<IList<RbacPermissionGrant>>(new List<RbacPermissionGrant>());
    }

    public Task<IList<RbacRoleGrant>> FindUserGroupRolesByUserId(string userId)
    {
        return Task.FromResult<IList<RbacRoleGrant>>(new List<RbacRoleGrant>());
    }

    public Task<List<RbacPermissionGrant>> FindGuestPermissions()
    {
        return Task.FromResult(_guestPermissions);
    }
}
