using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.RBAC.Dto;

public class RbacPermissionGrant
{
    public string Id { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public AssignedEntityType AssignedEntityType { get; set; } = AssignedEntityType.User;
    public string AssignedEntityId { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string Permission { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Resource { get; set; } = "";
}