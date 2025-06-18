namespace SelfService.Application;

public interface IRbacApplicationService
{
    Task<PermittedResponse> IsUserPermitted(string user, List<Permission> permissions, string objectId);
    List<AccessPolicy> GetApplicablePoliciesUser(string user);
}