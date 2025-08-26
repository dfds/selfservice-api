namespace SelfService.Domain.Models;

public class RbacNamespace : ValueObject
{
    public static readonly RbacNamespace Topics = new("topics");
    public static readonly RbacNamespace CapabilityManagement = new("capability-management");
    public static readonly RbacNamespace CapabilityMembershipManagement = new("capability-membership-management");
    public static readonly RbacNamespace TagsAndMetadata = new("tags-and-metadata");
    public static readonly RbacNamespace Aws = new("aws");
    public static readonly RbacNamespace Finout = new("finout");
    public static readonly RbacNamespace Azure = new("azure");
    public static readonly RbacNamespace RBAC = new("rbac");

    private readonly string _value;

    private RbacNamespace(string requested)
    {
        _value = requested;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    public override string ToString()
    {
        return _value;
    }

    public static RbacNamespace Parse(string text)
    {
        if (TryParse(text, out var ns))
        {
            return ns;
        }

        throw new FormatException($"Value \"{text}\" is not a valid RBAC namespace.");
    }

    public static bool TryParse(string input, out RbacNamespace rbacNamespace)
    {
        switch (input.ToLower())
        {
            case "topics":
                rbacNamespace = Topics;
                break;
            case "capability-management":
                rbacNamespace = CapabilityManagement;
                break;
            case "capability-membership-management":
                rbacNamespace = CapabilityMembershipManagement;
                break;
            case "tags-and-metadata":
                rbacNamespace = TagsAndMetadata;
                break;
            case "aws":
                rbacNamespace = Aws;
                break;
            case "finout":
                rbacNamespace = Finout;
                break;
            case "azure":
                rbacNamespace = Azure;
                break;
            case "rbac":
                rbacNamespace = RBAC;
                break;
            default:
                rbacNamespace = null!;
                return false;
        }

        return true;
    }
}
