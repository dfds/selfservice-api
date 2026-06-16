namespace SelfService.Infrastructure.Api.RBAC.Dto;

public class RegisterServicePrincipalRequest
{
    public string Id { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

// Provisions a regular (User) Member from the RBAC admin page after an admin picks an Azure AD
// tenant user that is not yet in selfservice. Id is the user's email/UPN.
public class ProvisionMemberRequest
{
    public string Id { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
}
