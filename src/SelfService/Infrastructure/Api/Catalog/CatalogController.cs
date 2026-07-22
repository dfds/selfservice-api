using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Infrastructure.Api.RBAC;

namespace SelfService.Infrastructure.Api.Catalog;

[Route("catalog")]
[RbacConfig(nameof(RbacObjectType.Global), "id")]
[Produces("application/json")]
[ApiController]
public class CatalogController : ControllerBase
{
    private readonly ICatalogApplicationService _catalogApplicationService;
    private readonly CatalogApiResourceFactory _apiResourceFactory;

    public CatalogController(
        ICatalogApplicationService catalogApplicationService,
        CatalogApiResourceFactory apiResourceFactory
    )
    {
        _catalogApplicationService = catalogApplicationService;
        _apiResourceFactory = apiResourceFactory;
    }

    [HttpGet("applications")]
    [RequiresPermission("service-catalogue", "service-catalogue-read")]
    [ProducesResponseType(typeof(CatalogApplicationsApiResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplications(
        [FromQuery] string? capabilityId,
        [FromQuery] string? @namespace,
        [FromQuery] string? kind,
        [FromQuery] string? q,
        [FromQuery] bool? hasDocs,
        CancellationToken cancellationToken
    )
    {
        var filters = new ApplicationFilters(
            CapabilityId: capabilityId,
            Namespace: @namespace,
            Kind: kind,
            Query: q,
            HasDocs: hasDocs
        );

        var result = await _catalogApplicationService.ListApplications(filters, cancellationToken);
        return Ok(_apiResourceFactory.ConvertApplications(result));
    }

    [HttpGet("namespaces")]
    [RequiresPermission("service-catalogue", "service-catalogue-read")]
    [ProducesResponseType(typeof(CatalogNamespacesApiResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNamespaces(CancellationToken cancellationToken)
    {
        var result = await _catalogApplicationService.ListNamespaces(cancellationToken);
        return Ok(_apiResourceFactory.ConvertNamespaces(result));
    }

    [HttpGet("dependencies")]
    [RequiresPermission("service-catalogue", "service-catalogue-read")]
    [ProducesResponseType(typeof(CatalogDependenciesApiResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDependencies(
        [FromQuery] string? @namespace,
        [FromQuery] string? type,
        CancellationToken cancellationToken
    )
    {
        var filters = new DependencyFilters(Namespace: @namespace, Type: type);
        var result = await _catalogApplicationService.GetDependencies(filters, cancellationToken);
        return Ok(_apiResourceFactory.ConvertDependencies(result));
    }
}
