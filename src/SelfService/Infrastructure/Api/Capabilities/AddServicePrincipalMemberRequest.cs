namespace SelfService.Infrastructure.Api.Capabilities;

public class AddServicePrincipalMemberRequest
{
    public string ServicePrincipalId { get; set; } = string.Empty;
    public string? AppDisplayName { get; set; }
}
