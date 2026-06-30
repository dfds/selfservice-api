using Microsoft.Identity.Web;

namespace SelfService.Infrastructure.Catalog;

public interface ICatalogTokenProvider
{
    /// <summary>
    /// Acquires an app-only access token for the configured catalog scope, or null when no
    /// scope is configured (local dev) — in which case the caller omits the Authorization header.
    /// </summary>
    Task<string?> GetAccessToken(CancellationToken cancellationToken = default);
}

/// <summary>
/// Thin wrapper over ITokenAcquisition.GetAccessTokenForAppAsync. selfservice-api acquires the
/// token as its own service principal; one shared app token is used across all clusters (only the
/// endpoint registry is per-cluster).
/// </summary>
public class CatalogTokenProvider : ICatalogTokenProvider
{
    private readonly CatalogConfig _config;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly ILogger<CatalogTokenProvider> _logger;

    public CatalogTokenProvider(
        CatalogConfig config,
        ITokenAcquisition tokenAcquisition,
        ILogger<CatalogTokenProvider> logger
    )
    {
        _config = config;
        _tokenAcquisition = tokenAcquisition;
        _logger = logger;
    }

    public async Task<string?> GetAccessToken(CancellationToken cancellationToken = default)
    {
        if (!_config.TokenAcquisitionEnabled)
        {
            // Local dev / unconfigured: no bearer token — ssu-catalog runs OIDC-disabled.
            return null;
        }

        try
        {
            return await _tokenAcquisition.GetAccessTokenForAppAsync(_config.Scope);
        }
        catch (Exception ex)
        {
            // Fail soft: a missing token means a 401 downstream, which the client treats as a
            // per-cluster failure (meta.catalogAvailable reflects it). Never throw here.
            _logger.LogError(ex, "Failed to acquire catalog access token for scope {Scope}", _config.Scope);
            return null;
        }
    }
}
