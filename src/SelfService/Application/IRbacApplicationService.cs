using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IRbacApplicationService
{
    Task<PermittedResponse> IsUserPermitted(string user, List<Permission> permissions, string objectId);
    List<AccessPolicy> GetApplicablePoliciesUser(string user);
    Task<List<RbacPermissionGrant>> GetPermissionGrantsForUser(string user);
    Task<List<RbacRoleGrant>> GetRoleGrantsForUser(string user);
}