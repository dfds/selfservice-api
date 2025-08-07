using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.RBAC.Dto;

public class RbacRoleGrant
{
    public string Id { get; set; } = "";
    public string RoleId { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public AssignedEntityType AssignedEntityType { get; set; } = AssignedEntityType.User;
    public string AssignedEntityId { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Resource { get; set; } = "";

    public SelfService.Domain.Models.RbacRoleGrant IntoDomainModel()
    {
        return SelfService.Domain.Models.RbacRoleGrant.New(RbacRoleId.Parse(RoleId), AssignedEntityType, AssignedEntityId, Type, Resource ?? "");
    }
}