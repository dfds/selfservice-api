using System.Text.Json;
using System.Text.Json.Serialization;

namespace SelfService.Domain.Models;

[JsonConverter(typeof(RbacNamespaceJsonConverter))]
public class RbacNamespace : ValueObject
{
    // topics, capability-management, capability-membership-management, tags-and-metadata, aws, finout, azure, rbac
    public static readonly RbacNamespace Topics = new("topics");
    public static readonly RbacNamespace TopicsPublic = new("topics-public");
    public static readonly RbacNamespace CapabilityManagement = new("capability-management");
    public static readonly RbacNamespace CapabilityMembershipManagement = new("capability-membership-management");
    public static readonly RbacNamespace TagsAndMetadata = new("tags-and-metadata");
    public static readonly RbacNamespace Aws = new("aws");
    public static readonly RbacNamespace Finout = new("finout");
    public static readonly RbacNamespace Azure = new("azure");
    public static readonly RbacNamespace Rbac = new("rbac");

    // allow non-optional values. Cannot be created and has no permissions.
    public static readonly RbacNamespace Default = new("default");

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
        if (TryParse(text, out var at))
        {
            return at;
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
            case "topics-public":
                rbacNamespace = TopicsPublic;
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
                rbacNamespace = Rbac;
                break;
            default:
                rbacNamespace = null!;
                return false;
        }

        return true;
    }
}

public class RbacNamespaceJsonConverter : JsonConverter<RbacNamespace>
{
    public override RbacNamespace? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var namespaceValue = reader.GetString();
        RbacNamespace.TryParse(namespaceValue ?? "", out var result);
        return result;
    }

    public override void Write(Utf8JsonWriter writer, RbacNamespace value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
