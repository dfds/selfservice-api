using System.Text.Json.Serialization;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Catalog;

// Response DTOs for the catalog HTTP surface. These are the SSU-facing shapes — the raw
// ssu-catalog wire DTOs (Infrastructure/Catalog/CatalogDtos.cs) are never returned directly.
// Property names serialize to camelCase (ASP.NET Core web defaults); `_links` follows the
// HATEOAS convention used across the API.

/// <summary>Availability summary surfaced in every catalog endpoint's meta envelope.</summary>
public class CatalogMetaApiResource
{
    public bool CatalogAvailable { get; init; }
    public int ClustersQueried { get; init; }
    public int ClustersFailed { get; init; }
}

public class ContainerApiResource
{
    public string Name { get; init; } = "";
    public string Image { get; init; } = "";
    public string ImageTag { get; init; } = "";
}

public class ServicePortApiResource
{
    public string Name { get; init; } = "";
    public int Port { get; init; }
    public string TargetPort { get; init; } = "";
    public string Protocol { get; init; } = "";
}

public class RouteApiResource
{
    public string Name { get; init; } = "";
    public string Kind { get; init; } = "";
    public IReadOnlyList<string> Hosts { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PathPrefixes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> EntryPoints { get; init; } = Array.Empty<string>();
    public bool Tls { get; init; }
}

public class ApiDocApiResource
{
    public int Port { get; init; }
    public string Path { get; init; } = "";
    public string Url { get; init; } = "";
    public bool ExternallyAvailable { get; init; }
    public string ExternalUrl { get; init; } = "";
}

public class ServiceApiResource
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public string ClusterIP { get; init; } = "";
    public IReadOnlyList<ServicePortApiResource> Ports { get; init; } = Array.Empty<ServicePortApiResource>();
    public IReadOnlyList<string> ExternalHosts { get; init; } = Array.Empty<string>();
    public IReadOnlyList<RouteApiResource> Routes { get; init; } = Array.Empty<RouteApiResource>();
    public IReadOnlyList<ApiDocApiResource> ApiDocs { get; init; } = Array.Empty<ApiDocApiResource>();
}

public class DeploymentSourceApiResource
{
    public string Tool { get; init; } = "";
    public string RepoUrl { get; init; } = "";
    public string Path { get; init; } = "";
    public string Revision { get; init; } = "";
    public string AppName { get; init; } = "";
}

public class KafkaTopicRefApiResource
{
    public string Name { get; init; } = "";
    public string Direction { get; init; } = "";
    public string Source { get; init; } = "";
}

public class DatabaseRefApiResource
{
    public string System { get; init; } = "";
    public string Name { get; init; } = "";
    public string Source { get; init; } = "";
}

/// <summary>A single workload (Deployment/StatefulSet/etc.) mapped from the catalog.</summary>
public class ApplicationApiResource
{
    public string Cluster { get; init; } = "";
    public string Namespace { get; init; } = "";
    public string Name { get; init; } = "";
    public string Kind { get; init; } = "";
    public string CapabilityId { get; init; } = "";
    public string? CapabilityName { get; init; }

    public int Replicas { get; init; }
    public int ReadyReplicas { get; init; }
    public IReadOnlyList<ContainerApiResource> Containers { get; init; } = Array.Empty<ContainerApiResource>();

    public string RepoUrl { get; init; } = "";
    public DeploymentSourceApiResource? DeploymentSource { get; init; }

    public string Owner { get; init; } = "";
    public string Contact { get; init; } = "";

    public IReadOnlyList<ServiceApiResource> Services { get; init; } = Array.Empty<ServiceApiResource>();
    public IReadOnlyList<KafkaTopicRefApiResource> KafkaTopics { get; init; } = Array.Empty<KafkaTopicRefApiResource>();
    public IReadOnlyList<DatabaseRefApiResource> Databases { get; init; } = Array.Empty<DatabaseRefApiResource>();

    [JsonPropertyName("_links")]
    public ApplicationLinks Links { get; init; } = new();

    public class ApplicationLinks
    {
        public ResourceLink? Capability { get; init; }
    }
}

public class NamespaceApiResource
{
    public string Cluster { get; init; } = "";
    public string Name { get; init; } = "";
    public string CapabilityId { get; init; } = "";
    public string? CapabilityName { get; init; }
    public string AwsAccountId { get; init; } = "";
    public string ContextId { get; init; } = "";
    public string CostCentre { get; init; } = "";
    public IReadOnlyDictionary<string, string>? Labels { get; init; }

    [JsonPropertyName("_links")]
    public NamespaceLinks Links { get; init; } = new();

    public class NamespaceLinks
    {
        public ResourceLink? Capability { get; init; }
    }
}

public class DependencyNodeApiResource
{
    public string Cluster { get; init; } = "";
    public string Namespace { get; init; } = "";
    public string Service { get; init; } = "";
    public bool External { get; init; }
}

public class DependencyApiResource
{
    public DependencyNodeApiResource Source { get; init; } = new();
    public DependencyNodeApiResource Target { get; init; } = new();
    public string Type { get; init; } = "";
    public string Origin { get; init; } = "";
    public string Details { get; init; } = "";
}

// ---- List envelopes: { data, meta, _links } ----

public class CatalogDeploymentsApiResource
{
    public IReadOnlyList<ApplicationApiResource> Data { get; init; } = Array.Empty<ApplicationApiResource>();
    public CatalogMetaApiResource Meta { get; init; } = new();

    [JsonPropertyName("_links")]
    public SelfLinks Links { get; init; } = new();
}

public class CatalogApplicationsApiResource
{
    public IReadOnlyList<ApplicationApiResource> Data { get; init; } = Array.Empty<ApplicationApiResource>();
    public CatalogMetaApiResource Meta { get; init; } = new();

    [JsonPropertyName("_links")]
    public SelfLinks Links { get; init; } = new();
}

public class CatalogNamespacesApiResource
{
    public IReadOnlyList<NamespaceApiResource> Data { get; init; } = Array.Empty<NamespaceApiResource>();
    public CatalogMetaApiResource Meta { get; init; } = new();

    [JsonPropertyName("_links")]
    public SelfLinks Links { get; init; } = new();
}

public class CatalogDependenciesApiResource
{
    public IReadOnlyList<DependencyApiResource> Data { get; init; } = Array.Empty<DependencyApiResource>();
    public CatalogMetaApiResource Meta { get; init; } = new();

    [JsonPropertyName("_links")]
    public SelfLinks Links { get; init; } = new();
}

public class SelfLinks
{
    public ResourceLink? Self { get; init; }
}
