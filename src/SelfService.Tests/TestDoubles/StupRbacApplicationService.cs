using Microsoft.VisualBasic;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Services;

namespace SelfService.Tests.TestDoubles;

public class StubRbacApplicationService : IRbacApplicationService
{
    private readonly bool _isPermitted;

    public StubRbacApplicationService(bool isPermitted)
    {
        _isPermitted = isPermitted;
    }

    public Task<RbacGroup> CreateGroup(string user, RbacGroup group)
    {
        throw new NotImplementedException();
    }

    public Task<RbacRole> CreateRole(string user, RbacRole role)
    {
        throw new NotImplementedException();
    }

    public Task DeleteGroup(string user, string groupId)
    {
        throw new NotImplementedException();
    }

    public Task DeleteRole(string user, string roleId)
    {
        throw new NotImplementedException();
    }

    public Task<List<RbacGroup>> GetGroupsForUser(string user)
    {
        throw new NotImplementedException();
    }

    public Task<List<RbacGroup>> GetSystemGroups()
    {
        throw new NotImplementedException();
    }

    public Task<List<RbacPermissionGrant>> GetPermissionGrantsForCapability(string capabilityId)
    {
        return Task.FromResult(new List<RbacPermissionGrant>());
    }

    public Task<List<RbacPermissionGrant>> GetPermissionGrantsForGroup(string groupId)
    {
        throw new NotImplementedException();
    }

    public Task<List<RbacPermissionGrant>> GetPermissionGrantsForRole(string roleId)
    {
        throw new NotImplementedException();
    }

    public Task<List<RbacPermissionGrant>> GetPermissionGrantsForRoleGrants(List<RbacRoleGrant> roleGrants)
    {
        throw new NotImplementedException();
    }

    public Task<List<RbacPermissionGrant>> GetPermissionGrantsForUser(string user)
    {
        throw new NotImplementedException();
    }

    public Task<List<RbacRoleGrant>> GetRoleGrantsForCapability(string capabilityId)
    {
        return Task.FromResult(new List<RbacRoleGrant>());
    }

    public Task<List<RbacRoleGrant>> GetRoleGrantsForGroup(string groupId)
    {
        throw new NotImplementedException();
    }

    public Task<List<RbacRoleGrant>> GetRoleGrantsForUser(string user)
    {
        throw new NotImplementedException();
    }

    public Task<List<RbacRole>> GetAssignableRoles()
    {
        return Task.FromResult(new List<RbacRole>());
    }

    public Task GrantPermission(string user, RbacPermissionGrant permissionGrant)
    {
        throw new NotImplementedException();
    }

    public Task GrantRoleGrant(string user, RbacRoleGrant roleGrant)
    {
        throw new NotImplementedException();
    }

    public Task RevokeCapabilityRoleGrant(UserId userId, CapabilityId capabilityId)
    {
        throw new NotImplementedException();
    }

    public Task RevokeGroupGrant(string user, RbacGroupMember membership)
    {
        throw new NotImplementedException();
    }

    public Task RevokePermission(string user, string id)
    {
        throw new NotImplementedException();
    }

    public Task RevokeRoleGrant(string user, string id)
    {
        throw new NotImplementedException();
    }

    public Task<PermittedResponse> IsUserPermitted(string user, List<Permission> permissions, string objectId)
    {
        var response = new PermittedResponse();
        response.PermissionMatrix = new Dictionary<string, PermissionMatrix>();

        var permissionMatrix = new PermissionMatrix(requestedPermission: new Permission());

        if (_isPermitted)
        {
            permissionMatrix.Permitted = true;
            response.PermissionMatrix[objectId] = permissionMatrix;
        }
        else
        {
            permissionMatrix.Permitted = false;
            response.PermissionMatrix[objectId] = permissionMatrix;
        }
        return Task.FromResult(response);
    }

    public Task<RbacGroupMember> GrantGroupGrant(string user, RbacGroupMember membership)
    {
        throw new NotImplementedException();
    }
}
