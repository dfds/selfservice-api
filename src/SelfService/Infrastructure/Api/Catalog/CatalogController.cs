using Microsoft.AspNetCore.Mvc;
using SelfService.Application;

namespace SelfService.Infrastructure.Api.Catalog;

/// <summary>
/// Cross-cluster, capability-scoped view of the ssu-catalog application catalog. All endpoints
/// are open reads (authenticated, no special permission) and always return 200 — when the
/// upstream catalog is unreachable the payload carries empty <c>data</c> plus
/// <c>meta.catalogAvailable = false</c> (the unavailability contract).
/// </summary>
[Route("catalog")]
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
    [ProducesResponseType(typeof(CatalogNamespacesApiResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNamespaces(CancellationToken cancellationToken)
    {
        var result = await _catalogApplicationService.ListNamespaces(cancellationToken);
        return Ok(_apiResourceFactory.ConvertNamespaces(result));
    }

    [HttpGet("dependencies")]
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
