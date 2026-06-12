namespace SelfService.Infrastructure.Api.RBAC.Dto;

public class RegisterServicePrincipalRequest
{
    public string Id { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}
