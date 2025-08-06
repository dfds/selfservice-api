using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Api.Capabilities;

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

    [HttpGet("get-assignable-permissions")]
    [ProducesResponseType(typeof(List<Permission>), StatusCodes.Status200OK)]
    public IActionResult GetAssignablePermissions()
    {
        return Ok(Permission.BootstrapPermissions());
    }

    [HttpPost("permission/grant")]
    public async Task<IActionResult> GrantPermission([FromBody] RbacPermissionGrant permissionGrant)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();
        await _rbacApplicationService.GrantPermission(userId.ToString(), permissionGrant);
        return Created();
    }
    
    [HttpDelete("permission/revoke")]
    public Task<IActionResult> RevokePermission()
    {
        return Task.FromResult<IActionResult>(Ok());
    }
    
    [HttpGet("permission/capability/{id:required}")]
    public Task<IActionResult> GetCapabilityPermissions()
    {
        return Task.FromResult<IActionResult>(Ok());
    }
    
    [HttpGet("permission/user/{id:required}")]
    public Task<IActionResult> GetUserPermissions()
    {
        return Task.FromResult<IActionResult>(Ok());
    }
    
    [HttpGet("permission/group/{id:required}")]
    public Task<IActionResult> GetGroupPermissions()
    {
        return Task.FromResult<IActionResult>(Ok());
    }
    
    [HttpPost("role/grant")]
    public Task<IActionResult> GrantRole()
    {
        return Task.FromResult<IActionResult>(Ok());
    }
    
    [HttpDelete("role/revoke")]
    public Task<IActionResult> RevokeRole()
    {
        return Task.FromResult<IActionResult>(Ok());
    }
    
    [HttpGet("role/capability/{id:required}")]
    public Task<IActionResult> GetCapabilityRoleGrants()
    {
        return Task.FromResult<IActionResult>(Ok());
    }
    
    [HttpGet("role/user/{id:required}")]
    public Task<IActionResult> GetUserRoleGrants()
    {
        return Task.FromResult<IActionResult>(Ok());
    }
    
    [HttpGet("role/group/{id:required}")]
    public Task<IActionResult> GetGroupRoleGrants()
    {
        return Task.FromResult<IActionResult>(Ok());
    }
    
    [HttpPost("can-i")]
    public Task<IActionResult> CanI()
    {
        return Task.FromResult<IActionResult>(Ok());
    }
    
    [HttpPost("can-they")]
    [RequiresPermission("rbac", "read")]
    public Task<IActionResult> CanThey()
    {
        return Task.FromResult<IActionResult>(Ok());
    }
}