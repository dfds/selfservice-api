using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IRbacApplicationService
{
    Task<PermittedResponse> IsUserPermitted(string user, List<Permission> permissions, string objectId);
    Task<List<RbacPermissionGrant>> GetPermissionGrantsForRoleGrants(List<RbacRoleGrant> roleGrants);
    Task<List<RbacPermissionGrant>> GetPermissionGrantsForUser(string user);
    Task<List<RbacRoleGrant>> GetRoleGrantsForUser(string user);
    Task<List<RbacPermissionGrant>> GetPermissionGrantsForGroup(string groupId);
    Task<List<RbacGroup>> GetGroupsForUser(string user);
}