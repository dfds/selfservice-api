using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Configuration;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Api.RBAC.Dto;
using RbacPermissionGrant = SelfService.Infrastructure.Api.RBAC.Dto.RbacPermissionGrant;
using RbacRoleGrant = SelfService.Infrastructure.Api.RBAC.Dto.RbacRoleGrant;

namespace SelfService.Infrastructure.Api.RBAC;

[Route("rbac")]
[Produces("application/json")]
[RbacConfig(nameof(RbacObjectType.Global), "id")]
[ApiController]
public class RbacController : ControllerBase
{
    private readonly IRbacApplicationService _rbacApplicationService;
    private readonly IPermissionQuery _permissionQuery;
    private readonly ApiResourceFactory _apiResourceFactory;
    private readonly IAuthorizationService _authorizationService;

    public RbacController(
        IRbacApplicationService rbacApplicationService,
        IPermissionQuery permissionQuery,
        ApiResourceFactory apiResourceFactory,
        IAuthorizationService authorizationService
    )
    {
        _rbacApplicationService = rbacApplicationService;
        _permissionQuery = permissionQuery;
        _apiResourceFactory = apiResourceFactory;
        _authorizationService = authorizationService; // [180925-andfris] used only for checking temporary isCloudEngineer permissions
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(RbacMeApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    [HttpGet("get-assignable-roles")]
    [ProducesResponseType(typeof(List<RbacRoleDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssignableRoles()
    {
        var roles = await _rbacApplicationService.GetAssignableRoles();
        List<RbacRoleDTO> toRbacDTO(List<RbacRole> roles)
        {
            return roles.Select(role => RbacRoleDTO.FromRbacRole(role)).ToList();
        }
        return Ok(toRbacDTO(roles));
    }

    [HttpPost("permission/grant")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GrantPermission([FromBody] RbacPermissionGrant permissionGrant)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        /* [24-09-2024-andfris] temporary: only cloud engineers can grant permissions
            Should be removed when we have a proper RBAC setup for managing RBAC itself
        */
        if (!_authorizationService.IsCloudEngineer(User.ToPortalUser()))
            return Unauthorized();

        await _rbacApplicationService.GrantPermission(
            userId.ToString(),
            Domain.Models.RbacPermissionGrant.New(
                permissionGrant.AssignedEntityType,
                permissionGrant.AssignedEntityId,
                permissionGrant.Namespace,
                permissionGrant.Permission,
                RbacAccessType.Parse(permissionGrant.Type),
                permissionGrant.Resource ?? ""
            )
        );
        return Created();
    }

    [HttpDelete("permission/revoke/{id:required}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequiresPermission("rbac", "delete")]
    public async Task<IActionResult> RevokePermission(string id)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (!_authorizationService.IsCloudEngineer(User.ToPortalUser()))
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
        var resp = await _rbacApplicationService.GetPermissionGrantsForGroup(id);
        var payload = resp.Select(x => _apiResourceFactory.Convert(x));

        return Ok(payload.ToList());
    }

    [HttpGet("permission/role/{id:required}")]
    [ProducesResponseType(typeof(List<RbacPermissionGrantApiResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRolePermissions(string id)
    {
        var resp = await _rbacApplicationService.GetPermissionGrantsForRole(id);
        var payload = resp.Select(x => _apiResourceFactory.Convert(x));

        return Ok(payload.ToList());
    }

    [HttpPost("role")]
    [ProducesResponseType(typeof(RbacRoleDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateRole([FromBody] RbacRoleCreationDTO roleDto)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        /* [24-09-2024-andfris] temporary: only cloud engineers can create roles
            Should be removed when we have a proper RBAC setup for managing RBAC itself
        */
        if (!_authorizationService.IsCloudEngineer(User.ToPortalUser()))
            return Unauthorized();

        var role = await _rbacApplicationService.CreateRole(
            userId.ToString(),
            RbacRole.New(
                ownerId: userId.ToString(),
                name: roleDto.Name,
                description: roleDto.Description,
                type: RbacAccessType.Parse(roleDto.Type)
            )
        );
        return Created(string.Empty, RbacRoleDTO.FromRbacRole(role));
    }

    [HttpDelete("role/{id:required}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequiresPermission("rbac", "delete")]
    public async Task<IActionResult> DeleteRole(string id)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        await _rbacApplicationService.DeleteRole(userId.ToString(), id);
        return Ok();
    }

    [HttpPost("role/grant")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GrantRole([FromBody] RbacRoleGrant roleGrant)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        /* [24-09-2024-andfris] temporary: only cloud engineers can grant roles
            Should be removed when we have a proper RBAC setup for managing RBAC itself
        */
        if (!_authorizationService.IsCloudEngineer(User.ToPortalUser()))
            return Unauthorized();

        await _rbacApplicationService.GrantRoleGrant(userId.ToString(), roleGrant.IntoDomainModel());
        return Created();
    }

    [HttpDelete("role/revoke/{id:required}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequiresPermission("rbac", "delete")]
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

    [HttpGet("role/groups/{id:required}")]
    [ProducesResponseType(typeof(List<RbacRoleGrantApiResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroupRoleGrants(string id)
    {
        var resp = await _rbacApplicationService.GetRoleGrantsForGroup(id);
        var payload = resp.Select(x => _apiResourceFactory.Convert(x));

        return Ok(payload.ToList());
    }

    [HttpGet("groups")]
    [ProducesResponseType(typeof(List<RbacGroupApiResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllGroups()
    {
        var groups = await _rbacApplicationService.GetSystemGroups();
        var payload = groups.Select(g => _apiResourceFactory.Convert(g));
        return Ok(payload.ToList());
    }

    [HttpPost("groups")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RbacGroupApiResource), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateGroup([FromBody] Dto.RbacGroupCreationDTO request)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        /* [24-09-2024-andfris] temporary: only cloud engineers can create groups
            Should be removed when we have a proper RBAC setup for managing RBAC itself
        */
        if (!_authorizationService.IsCloudEngineer(User.ToPortalUser()))
            return Unauthorized();

        var group = await _rbacApplicationService.CreateGroup(
            userId.ToString(),
            Domain.Models.RbacGroup.New(name: request.Name, description: request.Description, members: request.Members)
        );
        return Created(string.Empty, _apiResourceFactory.Convert(group));
    }

    [HttpDelete("groups/{id:required}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequiresPermission("rbac", "delete")]
    public async Task<IActionResult> DeleteGroup(string id)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        await _rbacApplicationService.DeleteGroup(userId.ToString(), id);
        return Ok();
    }

    [HttpPost("groups/{id:required}/members")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddGroupMember(string id, [FromBody] RbacGroupMemberCreationDTO request)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (!RbacGroupId.TryParse(id, out var groupId))
            return BadRequest("Invalid rbac group id");

        /* [24-09-2024-andfris] temporary: only cloud engineers can add group members
            Should be removed when we have a proper RBAC setup for managing RBAC itself
        */
        if (!_authorizationService.IsCloudEngineer(User.ToPortalUser()))
            return Unauthorized();

        var newGroup = RbacGroupMember.New(groupId: groupId, userId: request.UserId);

        var membership = await _rbacApplicationService.GrantGroupGrant(userId.ToString(), newGroup);
        return Created(string.Empty, membership);
    }

    [HttpDelete("groups/{id:required}/members/{memberId:required}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequiresPermission("rbac", "delete")]
    public async Task<IActionResult> RemoveGroupMember(string id, string memberId)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();
        if (!RbacGroupId.TryParse(id, out var groupId))
            return BadRequest("Invalid rbac group id");
        if (!UserId.TryParse(memberId, out var userIdToRemove))
            return BadRequest("Invalid user id");

        var membership = RbacGroupMember.New(groupId: groupId, userId: userIdToRemove.ToString());

        await _rbacApplicationService.RevokeGroupGrant(userId.ToString(), membership);
        return Ok();
    }

    [HttpPost("can-i")]
    [ProducesResponseType(typeof(RbacPermittedResponseApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CanI([FromBody] CanRequest request)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();
        var resp = await _rbacApplicationService.IsUserPermitted(userId, request.Permissions, request.Objectid);
        return Ok(_apiResourceFactory.Convert(resp));
    }

    [HttpPost("can-they")]
    [ProducesResponseType(typeof(RbacPermittedResponseApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequiresPermission("rbac", "read")]
    public async Task<IActionResult> CanThey([FromBody] CanRequest request)
    {
        var resp = await _rbacApplicationService.IsUserPermitted(request.UserId, request.Permissions, request.Objectid);
        return Ok(_apiResourceFactory.Convert(resp));
    }
}
