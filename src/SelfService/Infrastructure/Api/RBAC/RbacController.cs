using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Configuration;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api;
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
    private readonly IMemberQuery _memberQuery;
    private readonly IMemberRepository _memberRepository;
    private readonly IMemberApplicationService _memberApplicationService;
    private readonly IMembershipApplicationService _membershipApplicationService;
    private readonly ApiResourceFactory _apiResourceFactory;
    private readonly IAuthorizationService _authorizationService;

    public RbacController(
        IRbacApplicationService rbacApplicationService,
        IPermissionQuery permissionQuery,
        IMemberQuery memberQuery,
        IMemberRepository memberRepository,
        IMemberApplicationService memberApplicationService,
        IMembershipApplicationService membershipApplicationService,
        ApiResourceFactory apiResourceFactory,
        IAuthorizationService authorizationService
    )
    {
        _rbacApplicationService = rbacApplicationService;
        _permissionQuery = permissionQuery;
        _memberQuery = memberQuery;
        _memberRepository = memberRepository;
        _memberApplicationService = memberApplicationService;
        _membershipApplicationService = membershipApplicationService;
        _apiResourceFactory = apiResourceFactory;
        _authorizationService = authorizationService;
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
    [RequiresPermission("rbac", "create")]
    public async Task<IActionResult> GrantPermission([FromBody] RbacPermissionGrant permissionGrant)
    {
        if (!User.TryGetUserId(out var userId))
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

        await _rbacApplicationService.RevokePermission(userId.ToString(), id);
        return Ok();
    }

    [HttpGet("permission/capability/{id:required}")]
    [ProducesResponseType(typeof(List<RbacPermissionGrantApiResource>), StatusCodes.Status200OK)]
    [RequiresPermission("rbac", "read")]
    public async Task<IActionResult> GetCapabilityPermissions(string id)
    {
        var resp = await _rbacApplicationService.GetPermissionGrantsForCapability(id);
        var payload = resp.Select(x => _apiResourceFactory.Convert(x));

        return Ok(payload.ToList());
    }

    [HttpGet("permission/user/{id:required}")]
    [ProducesResponseType(typeof(List<RbacPermissionGrantApiResource>), StatusCodes.Status200OK)]
    [RequiresPermission("rbac", "read")]
    public async Task<IActionResult> GetUserPermissions(string id)
    {
        var resp = await _rbacApplicationService.GetPermissionGrantsForUser(id);
        var payload = resp.Select(x => _apiResourceFactory.Convert(x));

        return Ok(payload.ToList());
    }

    [HttpGet("permission/group/{id:required}")]
    [ProducesResponseType(typeof(List<RbacPermissionGrantApiResource>), StatusCodes.Status200OK)]
    [RequiresPermission("rbac", "read")]
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
    [RequiresPermission("rbac", "create")]
    public async Task<IActionResult> CreateRole([FromBody] RbacRoleCreationDTO roleDto)
    {
        if (!User.TryGetUserId(out var userId))
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
    [RequiresPermission("rbac", "create")]
    public async Task<IActionResult> GrantRole([FromBody] RbacRoleGrant roleGrant)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var domainGrant = roleGrant.IntoDomainModel();
        await _rbacApplicationService.GrantRoleGrant(userId.ToString(), domainGrant);
        await SyncMembershipOnCapabilityGrant(domainGrant);
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

        var revokedGrant = await _rbacApplicationService.RevokeRoleGrant(userId.ToString(), id);
        await SyncMembershipOnCapabilityRevoke(revokedGrant);
        return Ok();
    }

    [HttpGet("role/capability/{id:required}")]
    [ProducesResponseType(typeof(List<RbacRoleGrantApiResource>), StatusCodes.Status200OK)]
    //[RequiresPermission("rbac", "read")]
    public async Task<IActionResult> GetCapabilityRoleGrants(string id)
    {
        var resp = await _rbacApplicationService.GetRoleGrantsForCapability(id);
        var payload = resp.Select(x => _apiResourceFactory.Convert(x));
        return Ok(payload.ToList());
    }

    [HttpGet("role/user/{id:required}")]
    [ProducesResponseType(typeof(List<RbacRoleGrantApiResource>), StatusCodes.Status200OK)]
    //[RequiresPermission("rbac", "read")]
    public async Task<IActionResult> GetUserRoleGrants(string id)
    {
        var resp = await _rbacApplicationService.GetRoleGrantsForUser(id);
        var payload = resp.Select(x => _apiResourceFactory.Convert(x));

        return Ok(payload.ToList());
    }

    [HttpGet("role/groups/{id:required}")]
    [ProducesResponseType(typeof(List<RbacRoleGrantApiResource>), StatusCodes.Status200OK)]
    //[RequiresPermission("rbac", "read")]
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
    [RequiresPermission("rbac", "create")]
    public async Task<IActionResult> CreateGroup([FromBody] Dto.RbacGroupCreationDTO request)
    {
        if (!User.TryGetUserId(out var userId))
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
    [RequiresPermission("rbac", "create")]
    public async Task<IActionResult> AddGroupMember(string id, [FromBody] RbacGroupMemberCreationDTO request)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (!RbacGroupId.TryParse(id, out var groupId))
            return BadRequest("Invalid rbac group id");

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

    [HttpGet("members")]
    [ProducesResponseType(typeof(MemberSummaryListApiResource), StatusCodes.Status200OK)]
    [RequiresPermission("rbac", "read")]
    public async Task<IActionResult> SearchMembers(
        [FromQuery] string? type,
        [FromQuery] string? search,
        [FromQuery] int? limit,
        [FromQuery] int? offset
    )
    {
        MemberType? memberType = null;
        if (!string.IsNullOrWhiteSpace(type) && !string.Equals(type, "All", StringComparison.OrdinalIgnoreCase))
        {
            if (!Enum.TryParse<MemberType>(type, ignoreCase: true, out var parsed))
                return BadRequest($"Unknown member type \"{type}\". Expected: User, ServicePrincipal, All.");
            memberType = parsed;
        }

        var (members, total) = await _memberQuery.Search(memberType, search, limit ?? 50, offset ?? 0);
        var resource = new MemberSummaryListApiResource
        {
            Items = members.Select(m => MapMemberSummary(m, includeLinks: false)).ToList(),
            Total = total,
        };
        return Ok(resource);
    }

    [HttpPost("service-principals")]
    [ProducesResponseType(typeof(MemberSummaryApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MemberSummaryApiResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [RequiresPermission("rbac", "create")]
    public async Task<IActionResult> RegisterServicePrincipal([FromBody] RegisterServicePrincipalRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Id))
            return BadRequest("id is required");

        if (!UserId.TryParse(request.Id, out var servicePrincipalId))
            return BadRequest("id is not a valid service principal id");

        var existing = await _memberRepository.FindBy(servicePrincipalId);
        if (existing != null && existing.Type == MemberType.User)
        {
            return Conflict(
                new ProblemDetails
                {
                    Title = "Identifier already taken by a user",
                    Detail =
                        $"Member \"{servicePrincipalId}\" already exists as a user and cannot be registered as a service principal.",
                    Status = StatusCodes.Status409Conflict,
                }
            );
        }

        if (existing == null)
        {
            var syntheticEmail = ClaimsPrincipleExtensions.BuildSyntheticEmail(
                servicePrincipalId.ToString(),
                request.DisplayName
            );
            await _memberApplicationService.RegisterServicePrincipal(
                servicePrincipalId,
                syntheticEmail,
                request.DisplayName
            );
        }

        var member = await _memberRepository.FindBy(servicePrincipalId);
        if (member == null)
            return StatusCode(StatusCodes.Status500InternalServerError);

        var resource = MapMemberSummary(member, includeLinks: true);
        return existing == null ? StatusCode(StatusCodes.Status201Created, resource) : Ok(resource);
    }

    [HttpGet("members/{id:required}")]
    [ProducesResponseType(typeof(MemberSummaryApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequiresPermission("rbac", "read")]
    public async Task<IActionResult> GetMember(string id)
    {
        if (!UserId.TryParse(id, out var memberId))
            return BadRequest("Invalid member id");

        var member = await _memberRepository.FindBy(memberId);
        if (member == null)
            return NotFound();

        return Ok(MapMemberSummary(member, includeLinks: true));
    }

    [HttpGet("members/{id:required}/groups")]
    [ProducesResponseType(typeof(List<RbacGroupApiResource>), StatusCodes.Status200OK)]
    [RequiresPermission("rbac", "read")]
    public async Task<IActionResult> GetMemberGroups(string id)
    {
        if (!UserId.TryParse(id, out var memberId))
            return BadRequest("Invalid member id");

        var groups = await _rbacApplicationService.GetGroupsForUser(memberId.ToString());
        var payload = groups.Select(g => _apiResourceFactory.Convert(g)).ToList();
        return Ok(payload);
    }

    [HttpPost("permission/grant-bulk")]
    [ProducesResponseType(typeof(BulkPermissionGrantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequiresPermission("rbac", "create")]
    public async Task<IActionResult> GrantPermissionBulk([FromBody] BulkPermissionGrantRequest request)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (request?.Grants == null || request.Grants.Count == 0)
            return BadRequest("At least one grant is required");

        var domainGrants = request
            .Grants.Select(g =>
                Domain.Models.RbacPermissionGrant.New(
                    g.AssignedEntityType,
                    g.AssignedEntityId,
                    g.Namespace,
                    g.Permission,
                    RbacAccessType.Parse(g.Type),
                    g.Resource ?? ""
                )
            )
            .ToList();

        await _rbacApplicationService.GrantPermissions(userId.ToString(), domainGrants);

        var response = new BulkPermissionGrantResponse
        {
            Created = domainGrants.Select(g => _apiResourceFactory.Convert(g)).ToList(),
        };
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("role/grant-bulk")]
    [ProducesResponseType(typeof(BulkRoleGrantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequiresPermission("rbac", "create")]
    public async Task<IActionResult> GrantRoleBulk([FromBody] BulkRoleGrantRequest request)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (request?.Grants == null || request.Grants.Count == 0)
            return BadRequest("At least one grant is required");

        var domainGrants = request.Grants.Select(g => g.IntoDomainModel()).ToList();

        await _rbacApplicationService.GrantRoleGrants(userId.ToString(), domainGrants);

        foreach (var grant in domainGrants)
            await SyncMembershipOnCapabilityGrant(grant);

        var response = new BulkRoleGrantResponse
        {
            Created = domainGrants.Select(g => _apiResourceFactory.Convert(g)).ToList(),
        };
        return StatusCode(StatusCodes.Status201Created, response);
    }

    // When a capability-scoped role is granted to a user or service principal from the RBAC admin
    // page, mirror it as a capability membership so the principal shows up on the capability's
    // members list and in their own /me. Groups are excluded — only individual identities (users
    // and service principals, both carried as AssignedEntityType.User) become capability members.
    private async Task SyncMembershipOnCapabilityGrant(Domain.Models.RbacRoleGrant grant)
    {
        if (grant.Type != RbacAccessType.Capability || grant.AssignedEntityType != AssignedEntityType.User)
            return;
        if (string.IsNullOrWhiteSpace(grant.Resource) || !CapabilityId.TryParse(grant.Resource, out var capabilityId))
            return;
        if (!UserId.TryParse(grant.AssignedEntityId, out var entityId))
            return;

        var member = await _memberRepository.FindBy(entityId);
        if (member is { Type: MemberType.ServicePrincipal })
        {
            // Ensures the service-principal Member record exists, then creates the membership.
            await _membershipApplicationService.AddServicePrincipalMember(capabilityId, entityId, member.DisplayName);
        }
        else
        {
            await _membershipApplicationService.JoinCapability(capabilityId, entityId);
        }
        // Both membership methods swallow AlreadyHasActiveMembershipException, so re-granting is idempotent.
    }

    // Counterpart to the grant: when the last capability-scoped role for a user or service principal
    // is revoked, remove their capability membership too. Other capability roles keep them a member.
    //
    // KNOWN LIMITATION: this assumes the membership was created by SyncMembershipOnCapabilityGrant,
    // i.e. that membership is role-grant-derived. Memberships have no provenance tracking, so this
    // cannot distinguish a role-synced membership from one created independently — via the normal
    // membership-application flow, or via the explicit POST /{id}/service-principal-members endpoint.
    // When such a pre-existing member is granted then revoked a capability role, the grant is a no-op
    // (CreateAndAddMembership throws AlreadyHasActiveMembershipException, which is swallowed) but the
    // revoke still removes them here. Granting a role to an already-existing member is expected to be
    // rare; if it stops being rare, add a provenance flag to Membership and gate removal on it.
    private async Task SyncMembershipOnCapabilityRevoke(Domain.Models.RbacRoleGrant? revokedGrant)
    {
        if (revokedGrant is null)
            return;
        if (
            revokedGrant.Type != RbacAccessType.Capability
            || revokedGrant.AssignedEntityType != AssignedEntityType.User
        )
            return;
        if (
            string.IsNullOrWhiteSpace(revokedGrant.Resource)
            || !CapabilityId.TryParse(revokedGrant.Resource, out var capabilityId)
        )
            return;
        if (!UserId.TryParse(revokedGrant.AssignedEntityId, out var entityId))
            return;

        // The revoke has already committed, so the remaining grants reflect the post-revoke state.
        var remainingRoles = await _rbacApplicationService.GetRoleGrantsForUser(revokedGrant.AssignedEntityId);
        var stillHasCapabilityRole = remainingRoles.Any(r =>
            r.Type == RbacAccessType.Capability && r.Resource == revokedGrant.Resource
        );
        if (stillHasCapabilityRole)
            return;

        await _membershipApplicationService.RemoveMemberFromCapability(capabilityId, entityId);
    }

    private MemberSummaryApiResource MapMemberSummary(Member member, bool includeLinks)
    {
        var resource = new MemberSummaryApiResource
        {
            Id = member.Id.ToString(),
            Email = member.Email,
            DisplayName = member.DisplayName,
            Type = member.Type.ToString(),
            LastSeen = member.LastSeen,
        };

        if (includeLinks)
        {
            resource.Links = new MemberSummaryApiResource.MemberSummaryLinks
            {
                Self = new ResourceLink($"/rbac/members/{member.Id}", rel: "self", allow: Allow.Get),
                PermissionGrants = new ResourceLink(
                    $"/rbac/permission/user/{member.Id}",
                    rel: "permission-grants",
                    allow: Allow.Get + Method.Post
                ),
                RoleGrants = new ResourceLink(
                    $"/rbac/role/user/{member.Id}",
                    rel: "role-grants",
                    allow: Allow.Get + Method.Post
                ),
                Groups = new ResourceLink($"/rbac/members/{member.Id}/groups", rel: "groups", allow: Allow.Get),
            };
        }

        return resource;
    }

    [HttpPost("can-i")]
    [ProducesResponseType(typeof(RbacPermittedResponseApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CanI([FromBody] CanRequest? request)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();
        if (request is null)
            return BadRequest("Missing request body");

        var permissions = request.Permissions ?? new List<Permission>();
        var resp = await _rbacApplicationService.IsUserPermitted(userId, permissions, request.Objectid ?? "");
        return Ok(_apiResourceFactory.Convert(resp));
    }

    [HttpPost("can-they")]
    [ProducesResponseType(typeof(RbacPermittedResponseApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequiresPermission("rbac", "read")]
    public async Task<IActionResult> CanThey([FromBody] CanRequest? request)
    {
        if (request is null)
            return BadRequest("Missing request body");

        var permissions = request.Permissions ?? new List<Permission>();
        var resp = await _rbacApplicationService.IsUserPermitted(request.UserId, permissions, request.Objectid ?? "");
        return Ok(_apiResourceFactory.Convert(resp));
    }

    [HttpGet("permission-matrix")]
    [ProducesResponseType(typeof(PermissionMatrixResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPermissionMatrix()
    {
        if (!User.TryGetUserId(out _))
            return Unauthorized();

        var roles = await _rbacApplicationService.GetAllRoles();
        var permissions = Permission.BootstrapPermissions();

        var grants = new List<PermissionMatrixGrantDto>();
        foreach (var role in roles)
        {
            var roleGrants = await _rbacApplicationService.GetPermissionGrantsForRoleIgnoreCase(role.Id.ToString());
            grants.AddRange(
                roleGrants.Select(g => new PermissionMatrixGrantDto(
                    role.Id.ToString(),
                    g.Namespace.ToString(),
                    g.Permission
                ))
            );
        }

        return Ok(
            new PermissionMatrixResponse(
                roles.Select(RbacRoleDTO.FromRbacRole).ToList(),
                permissions
                    .Select(p => new PermissionDto(
                        p.Namespace.ToString(),
                        p.Name,
                        p.Description,
                        p.AccessType.ToString()
                    ))
                    .ToList(),
                grants
            )
        );
    }

    [HttpPut("permission-matrix/role/{roleId:required}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> SetRolePermissions(string roleId, [FromBody] SetRolePermissionsRequest request)
    {
        if (!_authorizationService.CanManagePermissionMatrix(User.ToPortalUser()))
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = "Only cloud engineers can modify the permission matrix.",
                }
            );

        var allPermissions = Permission.BootstrapPermissions();
        var unknownPermissions = request
            .Permissions.Where(p =>
                !allPermissions.Any(ap => ap.Namespace.ToString() == p.Namespace && ap.Name == p.Name)
            )
            .ToList();

        if (unknownPermissions.Any())
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Unknown permissions",
                    Detail =
                        $"The following permissions are not recognised: {string.Join(", ", unknownPermissions.Select(p => $"{p.Namespace}/{p.Name}"))}",
                }
            );

        var entries = request
            .Permissions.Select(p =>
            {
                var matching = allPermissions.First(ap => ap.Namespace.ToString() == p.Namespace && ap.Name == p.Name);
                return new RolePermissionEntry(matching.Namespace, matching.Name, matching.AccessType);
            })
            .ToList();

        await _rbacApplicationService.SetPermissionsForRole(roleId, entries);
        return NoContent();
    }
}

public record PermissionDto(string Namespace, string Name, string Description, string AccessType);

public record PermissionMatrixGrantDto(string RoleId, string Namespace, string Permission);

public record SetRolePermissionEntry(string Namespace, string Name);

public record SetRolePermissionsRequest(List<SetRolePermissionEntry> Permissions);

public record PermissionMatrixResponse(
    List<RbacRoleDTO> Roles,
    List<PermissionDto> Permissions,
    List<PermissionMatrixGrantDto> Grants
);
