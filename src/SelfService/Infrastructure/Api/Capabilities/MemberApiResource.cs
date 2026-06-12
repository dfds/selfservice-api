namespace SelfService.Infrastructure.Api.Capabilities;

public class MemberApiResource
{
    public string Id { get; set; }
    public string? Name { get; set; }
    public string Email { get; set; }
    public string Type { get; set; }
    public string? ServicePrincipalOid { get; set; }

    public MemberApiResource(string id, string? name, string email, string type, string? servicePrincipalOid)
    {
        Id = id;
        Name = name;
        Email = email;
        Type = type;
        ServicePrincipalOid = servicePrincipalOid;
    }
}
