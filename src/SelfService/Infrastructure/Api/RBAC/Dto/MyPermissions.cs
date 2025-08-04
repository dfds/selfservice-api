
namespace SelfService.Infrastructure.Api.RBAC.Dto;

public class MyPermissionsResponse
{
    public List<RbacPermissionGrant> PermissionGrants { get; set; }
    public List<RbacRoleGrant> RoleGrants { get; set; }

    public MyPermissionsResponse()
    {
        PermissionGrants = new List<RbacPermissionGrant>();
        RoleGrants = new List<RbacRoleGrant>();
    }
}