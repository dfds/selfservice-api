namespace SelfService.Domain.Models;

public class RbacAccessType : ValueObject
{
    public static readonly RbacAccessType Capability = new("capability");
    public static readonly RbacAccessType Global = new("global");
    public static readonly RbacAccessType Aws = new("aws");
    public static readonly RbacAccessType Azure = new("azure");

    private readonly string _value;

    private RbacAccessType(string requested)
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

    public static RbacAccessType Parse(string text)
    {
        if (TryParse(text, out var at))
        {
            return at;
        }

        throw new FormatException($"Value \"{text}\" is not a valid RBAC access type.");
    }

    public static bool TryParse(string input, out RbacAccessType rbacAccessType)
    {
        switch (input.ToLower())
        {
            case "capability":
                rbacAccessType = Capability;
                break;
            case "global":
                rbacAccessType = Global;
                break;
            case "aws":
                rbacAccessType = Aws;
                break;
            case "azure":
                rbacAccessType = Azure;
                break;
            default:
                rbacAccessType = null!;
                return false;
        }

        return true;
    }
}
