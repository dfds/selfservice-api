using Microsoft.Identity.Web;

namespace SelfService.Infrastructure.Catalog;

public interface ICatalogTokenProvider
{
    Task<string?> GetAccessToken(CancellationToken cancellationToken = default);
}

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
            _logger.LogError(ex, "Failed to acquire catalog access token for scope {Scope}", _config.Scope);
            return null;
        }
    }
}
