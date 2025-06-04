namespace SelfService.Application;

public interface IRbacApplicationService
{
    PermittedResponse IsUserPermitted(string user, List<Permission> permissions, string objectId);
    List<AccessPolicy> GetApplicablePoliciesUser(string user);
}