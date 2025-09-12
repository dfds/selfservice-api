using SelfService.Application;

namespace SelfService.Infrastructure.Api.RBAC.Dto;

public class CanRequest
{
    public List<Permission> Permissions { get; set; } = new List<Permission>();
    public string Objectid { get; set; } = "";
    public string UserId { get; set; } = "";
}
