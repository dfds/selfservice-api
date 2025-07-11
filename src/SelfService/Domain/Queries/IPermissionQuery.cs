using SelfService.Domain.Models;

namespace SelfService.Domain.Queries;

public interface IPermissionQuery
{
    Task<IList<RbacPermissionGrant>> FindUserGroupPermissionsByUserId(string userId);
    Task<IList<RbacRoleGrant>> FindUserGroupRolesByUserId(string userId);
}