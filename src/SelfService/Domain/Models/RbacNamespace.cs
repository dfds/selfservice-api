using System.Text.Json;
using System.Text.Json.Serialization;

namespace SelfService.Domain.Models;

[JsonConverter(typeof(RbacNamespaceJsonConverter))]
public class RbacNamespace : ValueObject
{
    public static readonly RbacNamespace Topics = new("topics");
    public static readonly RbacNamespace Capability = new("capability");
    public static readonly RbacNamespace TagsAndMetadata = new("tags-and-metadata");
    public static readonly RbacNamespace Aws = new("aws");
    public static readonly RbacNamespace Finout = new("finout");
    public static readonly RbacNamespace Azure = new("azure");
    public static readonly RbacNamespace Rbac = new("rbac");
    public static readonly RbacNamespace ServiceCatalogue = new("service-catalogue");
    public static readonly RbacNamespace Demos = new("demos");
    public static readonly RbacNamespace ReleaseNotes = new("release-notes");
    public static readonly RbacNamespace Events = new("events");
    public static readonly RbacNamespace News = new("news");
    public static readonly RbacNamespace Users = new("users");
    public static readonly RbacNamespace SelfAssessment = new("self-assessment");
    public static readonly RbacNamespace SystemAdmin = new("system-admin");
    public static readonly RbacNamespace SystemLegacy = new("system-legacy");

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
            case "capability":
                rbacNamespace = Capability;
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
            case "service-catalogue":
                rbacNamespace = ServiceCatalogue;
                break;
            case "demos":
                rbacNamespace = Demos;
                break;
            case "release-notes":
                rbacNamespace = ReleaseNotes;
                break;
            case "events":
                rbacNamespace = Events;
                break;
            case "news":
                rbacNamespace = News;
                break;
            case "users":
                rbacNamespace = Users;
                break;
            case "self-assessment":
                rbacNamespace = SelfAssessment;
                break;
            case "system-admin":
                rbacNamespace = SystemAdmin;
                break;
            case "system-legacy":
                rbacNamespace = SystemLegacy;
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
