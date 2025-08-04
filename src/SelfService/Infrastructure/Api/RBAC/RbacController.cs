using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Api.RBAC.Dto;

namespace SelfService.Infrastructure.Api.RBAC;

[Route("rbac")]
[Produces("application/json")]
[ApiController]
public class RbacController : ControllerBase
{
    private readonly IRbacApplicationService _rbacApplicationService;
    private readonly IPermissionQuery _permissionQuery;
    private readonly ApiResourceFactory _apiResourceFactory;
    

    public RbacController(IRbacApplicationService rbacApplicationService, IPermissionQuery permissionQuery, ApiResourceFactory apiResourceFactory)
    {
        _rbacApplicationService = rbacApplicationService;
        _permissionQuery = permissionQuery;
        _apiResourceFactory = apiResourceFactory;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(RbacMeApiResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me()
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        // user level
        var userPermissions = await _rbacApplicationService.GetPermissionGrantsForUser(userId.ToString());
        var userRoles = await _rbacApplicationService.GetRoleGrantsForUser(userId.ToString());
        // group level
        var groupPermissions = await _permissionQuery.FindUserGroupPermissionsByUserId(userId.ToString());
        var groupRoles = await _permissionQuery.FindUserGroupRolesByUserId(userId.ToString());

        var combinedPermissions = userPermissions.Concat(groupPermissions).ToList();
        var combinedRoles = userRoles.Concat(groupRoles).ToList();
        
        // role permissions
        var permissionsFromRoles = await _rbacApplicationService.GetPermissionGrantsForRoleGrants(combinedRoles);
        combinedPermissions.AddRange(permissionsFromRoles);

        var groups = await _rbacApplicationService.GetGroupsForUser(userId);

        return Ok(_apiResourceFactory.Convert(combinedPermissions, combinedRoles, groups));
    }
}