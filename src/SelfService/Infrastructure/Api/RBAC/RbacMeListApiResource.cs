using System.Text.Json.Serialization;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Api.RBAC.Dto;

namespace SelfService.Infrastructure.Api.RBAC;

public class RbacMeApiResource
{
    public RbacPermissionGrant[] PermissionGrants { get; set; }
    public RbacRoleGrant[] RoleGrants { get; set; }

    [JsonPropertyName("_links")]
    public RbacMeLinks Links { get; set; }

    public class RbacMeLinks
    {
        public ResourceLink Self { get; set; }

        public RbacMeLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public RbacMeApiResource(RbacPermissionGrant[] permissionGrants, RbacRoleGrant[] roleGrants, RbacMeLinks links)
    {
        PermissionGrants = permissionGrants;
        RoleGrants = roleGrants;
        Links = links;
    }
}
