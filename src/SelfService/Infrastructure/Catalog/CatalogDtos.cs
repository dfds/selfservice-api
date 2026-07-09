using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Catalog;

// DTOs mirroring ssu-catalog/internal/model/catalog.go. Deserialized with
// PropertyNameCaseInsensitive = true, so PascalCase properties map to the
// service's camelCase JSON without per-property attributes.
//
// Collection properties use null-coalescing setters. ssu-catalog is Go, where a
// nil slice marshals to JSON `null` (not `[]`) unless tagged omitempty. A property
// initializer (`= new()`) only applies when the JSON key is ABSENT; an explicit
// `null` value overwrites it. The coalescing setter guarantees these are never null
// after deserialization, so the mapper and merge/join code can enumerate freely.

/// <summary>Envelope returned by every ssu-catalog endpoint: { data, meta }.</summary>
public sealed class CatalogEnvelope<T>
{
    public T? Data { get; set; }
    public CatalogEnvelopeMeta? Meta { get; set; }
}

public sealed class CatalogEnvelopeMeta
{
    public DateTime CollectedAt { get; set; }
    public string Cluster { get; set; } = "";
}

/// <summary>Full per-cluster catalog snapshot (GET /api/v1/catalog).</summary>
public sealed class CatalogSnapshotDto
{
    private List<ApplicationEntryDto> _applications = new();
    private List<NamespaceEntryDto> _namespaces = new();
    private List<DependencyEdgeDto> _dependencies = new();

    public string Cluster { get; set; } = "";
    public List<ApplicationEntryDto> Applications
    {
        get => _applications;
        set => _applications = value ?? new();
    }
    public List<NamespaceEntryDto> Namespaces
    {
        get => _namespaces;
        set => _namespaces = value ?? new();
    }
    public List<DependencyEdgeDto> Dependencies
    {
        get => _dependencies;
        set => _dependencies = value ?? new();
    }
    public DateTime CollectedAt { get; set; }
    public DateTime PublishedAt { get; set; }
    public CatalogStatsDto? Stats { get; set; }
}

public sealed class CatalogStatsDto
{
    public int TotalApplications { get; set; }
    public int CapabilityOwnedApplications { get; set; }
    public int ApplicationsWithDocs { get; set; }
    public int ApplicationsWithDeploySource { get; set; }
    public int TotalDependencies { get; set; }
    public long CollectionDurationMs { get; set; }
}

public sealed class NamespaceEntryDto
{
    public string Cluster { get; set; } = "";
    public string Name { get; set; } = "";
    public string CapabilityId { get; set; } = "";
    public string AwsAccountId { get; set; } = "";
    public string ContextId { get; set; } = "";
    public string CostCentre { get; set; } = "";
    public Dictionary<string, string>? Labels { get; set; }

    /// <summary>Authoritative capability name, joined SSU-side (not part of the wire model).</summary>
    [JsonIgnore]
    public string? CapabilityName { get; set; }
}

public sealed class ApplicationEntryDto
{
    private List<ContainerInfoDto> _containers = new();
    private List<ServiceRefDto> _services = new();
    private List<KafkaTopicRefDto> _kafkaTopics = new();
    private List<DatabaseRefDto> _databases = new();
    private List<string> _repoUrls = new();

    // Identity / join keys
    public string Cluster { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public string CapabilityId { get; set; } = "";

    // Workload runtime
    public int Replicas { get; set; }
    public int ReadyReplicas { get; set; }
    public List<ContainerInfoDto> Containers
    {
        get => _containers;
        set => _containers = value ?? new();
    }
    
    public List<string> RepoUrls
    {
        get => _repoUrls;
        set => _repoUrls = value ?? new();
    }
    public DeploymentSourceDto? DeploymentSource { get; set; }
    
    public AppMetadataDto? Metadata { get; set; }

    // Best-effort owner (may be empty)
    public string Owner { get; set; } = "";
    public string Contact { get; set; } = "";

    // Attached networking / API surface
    public List<ServiceRefDto> Services
    {
        get => _services;
        set => _services = value ?? new();
    }

    // Observed runtime overlay (best-effort)
    public List<KafkaTopicRefDto> KafkaTopics
    {
        get => _kafkaTopics;
        set => _kafkaTopics = value ?? new();
    }
    public List<DatabaseRefDto> Databases
    {
        get => _databases;
        set => _databases = value ?? new();
    }

    public Dictionary<string, string>? Labels { get; set; }
    public Dictionary<string, string>? Annotations { get; set; }

    /// <summary>Beyla's detected runtime/language (e.g. "go", "dotnet"); empty/absent when undetected.</summary>
    public string? Runtime { get; set; }

    /// <summary>Inbound HTTP throughput (req/s), Beyla-observed; null/absent when no inbound HTTP seen.</summary>
    public double? RequestRate { get; set; }

    /// <summary>5xx share of inbound traffic (0..1); meaningful only with RequestRate &gt; 0.</summary>
    public double? ErrorRate { get; set; }

    /// <summary>Authoritative capability name, joined SSU-side (not part of the wire model).</summary>
    [JsonIgnore]
    public string? CapabilityName { get; set; }
}

public sealed class ServiceRefDto
{
    private List<ServicePortDto> _ports = new();
    private List<string> _externalHosts = new();
    private List<RouteRefDto> _routes = new();
    private List<ApiDocInfoDto> _apiDocs = new();
    private List<ReachabilityResultDto> _reachability = new();

    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string ClusterIP { get; set; } = "";
    public List<ServicePortDto> Ports
    {
        get => _ports;
        set => _ports = value ?? new();
    }
    public List<string> ExternalHosts
    {
        get => _externalHosts;
        set => _externalHosts = value ?? new();
    }
    public List<RouteRefDto> Routes
    {
        get => _routes;
        set => _routes = value ?? new();
    }
    public List<ApiDocInfoDto> ApiDocs
    {
        get => _apiDocs;
        set => _apiDocs = value ?? new();
    }
    // Serve-time reachability overlay from ssu-catalog; omitempty on the Go side,
    // so it arrives absent (→ empty) for hosts with no verdict.
    public List<ReachabilityResultDto> Reachability
    {
        get => _reachability;
        set => _reachability = value ?? new();
    }
}

public sealed class RouteRefDto
{
    private List<string> _hosts = new();
    private List<string> _pathPrefixes = new();
    private List<string> _entryPoints = new();

    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public List<string> Hosts
    {
        get => _hosts;
        set => _hosts = value ?? new();
    }
    public List<string> PathPrefixes
    {
        get => _pathPrefixes;
        set => _pathPrefixes = value ?? new();
    }
    public List<string> EntryPoints
    {
        get => _entryPoints;
        set => _entryPoints = value ?? new();
    }
    public bool Tls { get; set; }
}

public sealed class DeploymentSourceDto
{
    public string Tool { get; set; } = "";
    public string RepoUrl { get; set; } = "";
    public string Path { get; set; } = "";
    public string Revision { get; set; } = "";
    public string AppName { get; set; } = "";
}

/// <summary>Author-declared workload metadata (dfds.cloud/description + dfds.cloud/link.*).</summary>
public sealed class AppMetadataDto
{
    private List<LinkRefDto> _links = new();

    public string Description { get; set; } = "";
    public List<LinkRefDto> Links
    {
        get => _links;
        set => _links = value ?? new();
    }
}

public sealed class LinkRefDto
{
    public string Label { get; set; } = "";
    public string Url { get; set; } = "";
}

public sealed class ContainerInfoDto
{
    private List<ContainerPortDto> _ports = new();

    public string Name { get; set; } = "";
    public string Image { get; set; } = "";
    public string ImageTag { get; set; } = "";
    public List<ContainerPortDto> Ports
    {
        get => _ports;
        set => _ports = value ?? new();
    }
    public ResourceInfoDto? Resources { get; set; }
}

public sealed class ContainerPortDto
{
    public string Name { get; set; } = "";
    public int ContainerPort { get; set; }
    public string Protocol { get; set; } = "";
}

public sealed class ResourceInfoDto
{
    public string RequestsCpu { get; set; } = "";
    public string RequestsMemory { get; set; } = "";
    public string LimitsCpu { get; set; } = "";
    public string LimitsMemory { get; set; } = "";
}

public sealed class ServicePortDto
{
    public string Name { get; set; } = "";
    public int Port { get; set; }
    public string TargetPort { get; set; } = "";
    public string Protocol { get; set; } = "";
}

public sealed class ApiDocInfoDto
{
    public int Port { get; set; }
    public string Path { get; set; } = "";
    public string Url { get; set; } = "";
    public bool ExternallyAvailable { get; set; }
    public string ExternalUrl { get; set; } = "";
}

/// <summary>Active ingress-reachability verdict per exposed host (ssu-catalog serve-time overlay).</summary>
public sealed class ReachabilityResultDto
{
    public string Host { get; set; } = "";
    public string Url { get; set; } = "";
    public string Status { get; set; } = ""; // "reachable" | "unreachable" | "unknown"
    public int StatusCode { get; set; }
    public string Expected { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTime CheckedAt { get; set; }
}

public sealed class KafkaTopicRefDto
{
    public string Name { get; set; } = "";
    public string Direction { get; set; } = "";
    public string Source { get; set; } = "";
}

public sealed class DatabaseRefDto
{
    public string System { get; set; } = "";
    public string Name { get; set; } = "";
    public string Source { get; set; } = "";
}

public sealed class DependencyEdgeDto
{
    public DependencyNodeDto Source { get; set; } = new();
    public DependencyNodeDto Target { get; set; } = new();
    public string Type { get; set; } = "";
    public string Origin { get; set; } = "";
    public string Details { get; set; } = "";
}

public sealed class DependencyNodeDto
{
    public string Cluster { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string Service { get; set; } = "";
    public bool External { get; set; }
}
