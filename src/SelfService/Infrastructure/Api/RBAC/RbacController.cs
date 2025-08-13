using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Api.RBAC.Dto;
using RbacRoleGrant = SelfService.Infrastructure.Api.RBAC.Dto.RbacRoleGrant;

namespace SelfService.Infrastructure.Api.RBAC;

[Route("rbac")]
[Produces("application/json")]
[RbacConfig(nameof(RbacObjectType.Global) ,"id")]
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
        await _rbacApplicationService.GrantPermission(userId.ToString(), Domain.Models.RbacPermissionGrant.New(permissionGrant.AssignedEntityType, permissionGrant.AssignedEntityId, permissionGrant.Namespace, permissionGrant.Permission, permissionGrant.Type, permissionGrant.Resource ?? ""));
        return Created();
    }
    
    [HttpDelete("permission/revoke/{id:required}")]
    public async Task<IActionResult> RevokePermission(string id)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();
        await _rbacApplicationService.RevokePermission(userId.ToString(), id);
        return Ok();
    }
    
    [HttpGet("permission/capability/{id:required}")]
    [ProducesResponseType(typeof(List<RbacPermissionGrantApiResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCapabilityPermissions(string id)
    {
        var resp = await _rbacApplicationService.GetPermissionGrantsForCapability(id);
        var payload = resp.Select(x => _apiResourceFactory.Convert(x));
        
        return Ok(payload.ToList());
    }
    
    [HttpGet("permission/user/{id:required}")]
    [ProducesResponseType(typeof(List<RbacPermissionGrantApiResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserPermissions(string id)
    {
        var resp = await _rbacApplicationService.GetPermissionGrantsForUser(id);
        var payload = resp.Select(x => _apiResourceFactory.Convert(x));
        
        return Ok(payload.ToList());
    }
    
    [HttpGet("permission/group/{id:required}")]
    [ProducesResponseType(typeof(List<RbacPermissionGrantApiResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroupPermissions(string id)
    {
        var resp = await _rbacApplicationService.GetPermissionGrantsForGroup(id.ToUpper());
        var payload = resp.Select(x => _apiResourceFactory.Convert(x));
        
        return Ok(payload.ToList());
    }
    
    [HttpPost("role/grant")]
    public async Task<IActionResult> GrantRole([FromBody] RbacRoleGrant roleGrant)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();
        await _rbacApplicationService.GrantRoleGrant(userId.ToString(), roleGrant.IntoDomainModel());
        return Created();
    }
    
    [HttpDelete("role/revoke/{id:required}")]
    public async Task<IActionResult> RevokeRole(string id)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();
        await _rbacApplicationService.RevokeRoleGrant(userId.ToString(), id);
        return Ok();
    }
    
    [HttpGet("role/capability/{id:required}")]
    [ProducesResponseType(typeof(List<RbacRoleGrantApiResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCapabilityRoleGrants(string id)
    {
        var resp = await _rbacApplicationService.GetRoleGrantsForCapability(id);
        var payload = resp.Select(x => _apiResourceFactory.Convert(x));
        
        return Ok(payload.ToList());
    }
    
    [HttpGet("role/user/{id:required}")]
    [ProducesResponseType(typeof(List<RbacRoleGrantApiResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRoleGrants(string id)
    {
        var resp = await _rbacApplicationService.GetRoleGrantsForUser(id);
        var payload = resp.Select(x => _apiResourceFactory.Convert(x));
        
        return Ok(payload.ToList());
    }
    
    [HttpGet("role/group/{id:required}")]
    [ProducesResponseType(typeof(List<RbacRoleGrantApiResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroupRoleGrants(string id)
    {
        var resp = await _rbacApplicationService.GetRoleGrantsForGroup(id.ToUpper());
        var payload = resp.Select(x => _apiResourceFactory.Convert(x));
        
        return Ok(payload.ToList());
    }
    
    [HttpPost("can-i")]
    [ProducesResponseType(typeof(RbacPermittedResponseApiResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> CanI([FromBody] CanRequest request)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();
        var resp = await _rbacApplicationService.IsUserPermitted(userId, request.Permissions, request.Objectid);
        return Ok(_apiResourceFactory.Convert(resp));
    }
    
    [HttpPost("can-they")]
    [ProducesResponseType(typeof(RbacPermittedResponseApiResource), StatusCodes.Status200OK)]
    [RequiresPermission("rbac", "read")]
    public async Task<IActionResult> CanThey([FromBody] CanRequest request)
    {
        var resp = await _rbacApplicationService.IsUserPermitted(request.UserId, request.Permissions, request.Objectid);
        return Ok(_apiResourceFactory.Convert(resp));
    }
}