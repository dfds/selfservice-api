using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.RBAC.Dto;

public class RbacGroup
{
    public string Id { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public ICollection<RbacGroupMember> Members { get; set; } = new List<RbacGroupMember>();
}

public class RbacGroupCreationDTO
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public ICollection<RbacGroupMember> Members { get; set; } = new List<RbacGroupMember>();

    public RbacGroupCreationDTO(string name, string description, ICollection<RbacGroupMember>? members = null)
    {
        Name = name;
        Description = description;
        Members = members ?? new List<RbacGroupMember>();
    }
}
