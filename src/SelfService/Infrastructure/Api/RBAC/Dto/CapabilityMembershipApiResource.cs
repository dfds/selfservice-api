namespace SelfService.Infrastructure.Api.RBAC.Dto;

public class CapabilityMembershipApiResource
{
    public string CapabilityId { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class GrantCapabilityMembershipRequest
{
    public string CapabilityId { get; set; } = string.Empty;
}
