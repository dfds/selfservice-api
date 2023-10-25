using SelfService.Domain.Models;

namespace SelfService.Domain;

public class AclResourceType : ValueObjectEnum<AclResourceType>
{
    public static readonly AclResourceType Cluster = new("CLUSTER");
    public static readonly AclResourceType Topic = new("TOPIC");
    public static readonly AclResourceType Group = new("GROUP");

    public AclResourceType(string value)
        : base(value) { }
}

public class AclPermissionType : ValueObjectEnum<AclPermissionType>
{
    public static readonly AclPermissionType Allow = new("ALLOW");
    public static readonly AclPermissionType Deny = new("DENY");

    private AclPermissionType(string value)
        : base(value) { }
}

public class AclPatternType : ValueObjectEnum<AclPatternType>
{
    public static readonly AclPatternType Literal = new("LITERAL");
    public static readonly AclPatternType Prefixed = new("PREFIXED");

    private AclPatternType(string value)
        : base(value) { }
}

public class AclOperationType : ValueObjectEnum<AclOperationType>
{
    public static readonly AclOperationType Create = new("CREATE");
    public static readonly AclOperationType Read = new("READ");
    public static readonly AclOperationType Write = new("WRITE");
    public static readonly AclOperationType Describe = new("DESCRIBE");
    public static readonly AclOperationType DescribeConfigs = new("DESCRIBE_CONFIGS");
    public static readonly AclOperationType Alter = new("ALTER");
    public static readonly AclOperationType AlterConfigs = new("ALTER_CONFIGS");
    public static readonly AclOperationType ClusterAction = new("CLUSTER_ACTION");

    private AclOperationType(string value)
        : base(value) { }
}

public class CreateAclRequestData
{
    public required AclResourceType ResourceType { get; set; }
    public required string ResourceName { get; set; }
    public required AclPatternType PatternType { get; set; }
    public required string Principal { get; set; }
    public required string Host { get; set; }
    public required AclOperationType Operation { get; set; }
    public required AclPermissionType Permission { get; set; }
}

public static class ConfluentCloudAclHelper
{
    public static CreateAclRequestData[] GetAcls(CapabilityId capabilityId, string serviceAccountId)
    {
        var publicTopicPrefix = "pub.";
        var clusterResourceName = "kafka-cluster";

        var capabilityPrefix = capabilityId.ToString();
        var publicCapabilityPrefix = $"{publicTopicPrefix}{capabilityPrefix}";
        var connectPrefix = $"connect-{capabilityPrefix}";

        var host = "*";
        var principal = $"User:{serviceAccountId}";

        return new CreateAclRequestData[]
        {
            new()
            {
                ResourceType = AclResourceType.Topic,
                ResourceName = capabilityPrefix,
                PatternType = AclPatternType.Prefixed,
                Operation = AclOperationType.Read,
                Permission = AclPermissionType.Allow,
                Principal = principal,
                Host = host
            },
            new()
            {
                ResourceType = AclResourceType.Topic,
                ResourceName = capabilityPrefix,
                PatternType = AclPatternType.Prefixed,
                Operation = AclOperationType.Write,
                Permission = AclPermissionType.Allow,
                Principal = principal,
                Host = host
            },
            new()
            {
                ResourceType = AclResourceType.Topic,
                ResourceName = capabilityPrefix,
                PatternType = AclPatternType.Prefixed,
                Operation = AclOperationType.Create,
                Permission = AclPermissionType.Allow,
                Principal = principal,
                Host = host
            },
            new()
            {
                ResourceType = AclResourceType.Topic,
                ResourceName = capabilityPrefix,
                PatternType = AclPatternType.Prefixed,
                Operation = AclOperationType.Describe,
                Permission = AclPermissionType.Allow,
                Principal = principal,
                Host = host
            },
            new()
            {
                ResourceType = AclResourceType.Topic,
                ResourceName = capabilityPrefix,
                PatternType = AclPatternType.Prefixed,
                Operation = AclOperationType.DescribeConfigs,
                Permission = AclPermissionType.Allow,
                Principal = principal,
                Host = host
            },
            new()
            {
                ResourceType = AclResourceType.Topic,
                ResourceName = publicTopicPrefix,
                PatternType = AclPatternType.Prefixed,
                Operation = AclOperationType.Read,
                Permission = AclPermissionType.Allow,
                Principal = principal,
                Host = host
            },
            new()
            {
                ResourceType = AclResourceType.Topic,
                ResourceName = publicCapabilityPrefix,
                PatternType = AclPatternType.Prefixed,
                Operation = AclOperationType.Write,
                Permission = AclPermissionType.Allow,
                Principal = principal,
                Host = host
            },
            new()
            {
                ResourceType = AclResourceType.Topic,
                ResourceName = publicCapabilityPrefix,
                PatternType = AclPatternType.Prefixed,
                Operation = AclOperationType.Create,
                Permission = AclPermissionType.Allow,
                Principal = principal,
                Host = host
            },
            new()
            {
                ResourceType = AclResourceType.Group,
                ResourceName = connectPrefix,
                PatternType = AclPatternType.Prefixed,
                Operation = AclOperationType.Read,
                Permission = AclPermissionType.Allow,
                Principal = principal,
                Host = host
            },
            new()
            {
                ResourceType = AclResourceType.Group,
                ResourceName = connectPrefix,
                PatternType = AclPatternType.Prefixed,
                Operation = AclOperationType.Write,
                Permission = AclPermissionType.Allow,
                Principal = principal,
                Host = host
            },
            new()
            {
                ResourceType = AclResourceType.Group,
                ResourceName = connectPrefix,
                PatternType = AclPatternType.Prefixed,
                Operation = AclOperationType.Create,
                Permission = AclPermissionType.Allow,
                Principal = principal,
                Host = host
            },
            new()
            {
                ResourceType = AclResourceType.Group,
                ResourceName = capabilityPrefix,
                PatternType = AclPatternType.Prefixed,
                Operation = AclOperationType.Read,
                Permission = AclPermissionType.Allow,
                Principal = principal,
                Host = host
            },
            new()
            {
                ResourceType = AclResourceType.Group,
                ResourceName = capabilityPrefix,
                PatternType = AclPatternType.Prefixed,
                Operation = AclOperationType.Write,
                Permission = AclPermissionType.Allow,
                Principal = principal,
                Host = host
            },
            new()
            {
                ResourceType = AclResourceType.Group,
                ResourceName = capabilityPrefix,
                PatternType = AclPatternType.Prefixed,
                Operation = AclOperationType.Create,
                Permission = AclPermissionType.Allow,
                Principal = principal,
                Host = host
            },
            new()
            {
                ResourceType = AclResourceType.Cluster,
                ResourceName = clusterResourceName,
                PatternType = AclPatternType.Literal,
                Operation = AclOperationType.Alter,
                Permission = AclPermissionType.Deny,
                Principal = principal,
                Host = host
            },
            new()
            {
                ResourceType = AclResourceType.Cluster,
                ResourceName = clusterResourceName,
                PatternType = AclPatternType.Literal,
                Operation = AclOperationType.AlterConfigs,
                Permission = AclPermissionType.Deny,
                Principal = principal,
                Host = host
            },
            new()
            {
                ResourceType = AclResourceType.Cluster,
                ResourceName = clusterResourceName,
                PatternType = AclPatternType.Literal,
                Operation = AclOperationType.ClusterAction,
                Permission = AclPermissionType.Deny,
                Principal = principal,
                Host = host
            }
        };
    }
}
