using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Catalog;

namespace SelfService.Infrastructure.Api.Catalog;

/// <summary>
/// Maps merged catalog results (<see cref="CatalogResult{T}"/> over the wire DTOs) into the
/// SSU-facing API resources, attaching HATEOAS links via <see cref="LinkGenerator"/>.
/// </summary>
public class CatalogApiResourceFactory
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;

    public CatalogApiResourceFactory(IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator)
    {
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
    }

    private HttpContext HttpContext =>
        _httpContextAccessor.HttpContext ?? throw new ApplicationException("Not in a http request context!");

    private static string GetNameOf<TController>()
        where TController : ControllerBase => typeof(TController).Name.Replace("Controller", "");

    public CatalogDeploymentsApiResource ConvertDeployments(
        CapabilityId capabilityId,
        CatalogResult<ApplicationEntryDto> result
    )
    {
        return new CatalogDeploymentsApiResource
        {
            Data = result.Items.Select(MapApplication).ToList(),
            Meta = MapMeta(result.Availability),
            Links = new SelfLinks
            {
                Self = new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(CapabilityController.GetCapabilityDeployments),
                        controller: GetNameOf<CapabilityController>(),
                        values: new { id = capabilityId.ToString() }
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                ),
            },
        };
    }

    public CatalogApplicationsApiResource ConvertApplications(CatalogResult<ApplicationEntryDto> result)
    {
        return new CatalogApplicationsApiResource
        {
            Data = result.Items.Select(MapApplication).ToList(),
            Meta = MapMeta(result.Availability),
            Links = new SelfLinks { Self = SelfLinkFor(nameof(CatalogController.GetApplications)) },
        };
    }

    public CatalogNamespacesApiResource ConvertNamespaces(CatalogResult<NamespaceEntryDto> result)
    {
        return new CatalogNamespacesApiResource
        {
            Data = result.Items.Select(MapNamespace).ToList(),
            Meta = MapMeta(result.Availability),
            Links = new SelfLinks { Self = SelfLinkFor(nameof(CatalogController.GetNamespaces)) },
        };
    }

    public CatalogDependenciesApiResource ConvertDependencies(CatalogResult<DependencyEdgeDto> result)
    {
        return new CatalogDependenciesApiResource
        {
            Data = result.Items.Select(MapDependency).ToList(),
            Meta = MapMeta(result.Availability),
            Links = new SelfLinks { Self = SelfLinkFor(nameof(CatalogController.GetDependencies)) },
        };
    }

    private ResourceLink SelfLinkFor(string action) =>
        new(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: action,
                controller: GetNameOf<CatalogController>()
            ) ?? "",
            rel: "self",
            allow: Allow.Get
        );

    private static CatalogMetaApiResource MapMeta(CatalogAvailability availability) =>
        new()
        {
            CatalogAvailable = availability.CatalogAvailable,
            ClustersQueried = availability.ClustersQueried,
            ClustersFailed = availability.ClustersFailed,
        };

    private ApplicationApiResource MapApplication(ApplicationEntryDto app) =>
        new()
        {
            Cluster = app.Cluster,
            Namespace = app.Namespace,
            Name = app.Name,
            Kind = app.Kind,
            CapabilityId = app.CapabilityId,
            CapabilityName = app.CapabilityName,
            Replicas = app.Replicas,
            ReadyReplicas = app.ReadyReplicas,
            Containers = app
                .Containers.Select(c => new ContainerApiResource
                {
                    Name = c.Name,
                    Image = c.Image,
                    ImageTag = c.ImageTag,
                })
                .ToList(),
            RepoUrl = app.RepoUrl,
            DeploymentSource = app.DeploymentSource is null
                ? null
                : new DeploymentSourceApiResource
                {
                    Tool = app.DeploymentSource.Tool,
                    RepoUrl = app.DeploymentSource.RepoUrl,
                    Path = app.DeploymentSource.Path,
                    Revision = app.DeploymentSource.Revision,
                    AppName = app.DeploymentSource.AppName,
                },
            Owner = app.Owner,
            Contact = app.Contact,
            Services = app.Services.Select(MapService).ToList(),
            KafkaTopics = app
                .KafkaTopics.Select(k => new KafkaTopicRefApiResource
                {
                    Name = k.Name,
                    Direction = k.Direction,
                    Source = k.Source,
                })
                .ToList(),
            Databases = app
                .Databases.Select(d => new DatabaseRefApiResource
                {
                    System = d.System,
                    Name = d.Name,
                    Source = d.Source,
                })
                .ToList(),
            Links = new ApplicationApiResource.ApplicationLinks { Capability = CapabilityLink(app.CapabilityId) },
        };

    private static ServiceApiResource MapService(ServiceRefDto svc) =>
        new()
        {
            Name = svc.Name,
            Type = svc.Type,
            ClusterIP = svc.ClusterIP,
            Ports = svc
                .Ports.Select(p => new ServicePortApiResource
                {
                    Name = p.Name,
                    Port = p.Port,
                    TargetPort = p.TargetPort,
                    Protocol = p.Protocol,
                })
                .ToList(),
            ExternalHosts = svc.ExternalHosts.ToList(),
            Routes = svc
                .Routes.Select(r => new RouteApiResource
                {
                    Name = r.Name,
                    Kind = r.Kind,
                    Hosts = r.Hosts.ToList(),
                    PathPrefixes = r.PathPrefixes.ToList(),
                    EntryPoints = r.EntryPoints.ToList(),
                    Tls = r.Tls,
                })
                .ToList(),
            ApiDocs = svc
                .ApiDocs.Select(d => new ApiDocApiResource
                {
                    Port = d.Port,
                    Path = d.Path,
                    Url = d.Url,
                    ExternallyAvailable = d.ExternallyAvailable,
                    ExternalUrl = d.ExternalUrl,
                })
                .ToList(),
        };

    private NamespaceApiResource MapNamespace(NamespaceEntryDto ns) =>
        new()
        {
            Cluster = ns.Cluster,
            Name = ns.Name,
            CapabilityId = ns.CapabilityId,
            CapabilityName = ns.CapabilityName,
            AwsAccountId = ns.AwsAccountId,
            ContextId = ns.ContextId,
            CostCentre = ns.CostCentre,
            Labels = ns.Labels,
            Links = new NamespaceApiResource.NamespaceLinks { Capability = CapabilityLink(ns.CapabilityId) },
        };

    private static DependencyApiResource MapDependency(DependencyEdgeDto edge) =>
        new()
        {
            Source = MapNode(edge.Source),
            Target = MapNode(edge.Target),
            Type = edge.Type,
            Origin = edge.Origin,
            Details = edge.Details,
        };

    private static DependencyNodeApiResource MapNode(DependencyNodeDto node) =>
        new()
        {
            Cluster = node.Cluster,
            Namespace = node.Namespace,
            Service = node.Service,
            External = node.External,
        };

    private ResourceLink? CapabilityLink(string capabilityId)
    {
        if (!CapabilityId.TryParse(capabilityId, out var id))
        {
            return null;
        }

        return new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.GetCapabilityById),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = id.ToString() }
            ) ?? "",
            rel: "related",
            allow: Allow.Get
        );
    }
}
