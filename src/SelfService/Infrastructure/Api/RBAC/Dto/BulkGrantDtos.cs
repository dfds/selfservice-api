namespace SelfService.Infrastructure.Api.RBAC.Dto;

public class BulkPermissionGrantRequest
{
    public List<RbacPermissionGrant> Grants { get; set; } = new();
}

public class BulkRoleGrantRequest
{
    public List<RbacRoleGrant> Grants { get; set; } = new();
}

public class BulkGrantFailure<T>
{
    public T Input { get; set; }
    public string Reason { get; set; } = "";

    public BulkGrantFailure(T input, string reason)
    {
        Input = input;
        Reason = reason;
    }
}

public class BulkPermissionGrantResponse
{
    public List<RbacPermissionGrantApiResource> Created { get; set; } = new();
    public List<BulkGrantFailure<RbacPermissionGrant>> Failed { get; set; } = new();
}

public class BulkRoleGrantResponse
{
    public List<RbacRoleGrantApiResource> Created { get; set; } = new();
    public List<BulkGrantFailure<RbacRoleGrant>> Failed { get; set; } = new();
}
