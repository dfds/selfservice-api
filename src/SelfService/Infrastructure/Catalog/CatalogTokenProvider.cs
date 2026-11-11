using Microsoft.Identity.Web;

namespace SelfService.Infrastructure.Catalog;

public interface ICatalogTokenProvider
{
    Task<string?> GetAccessToken(CancellationToken cancellationToken = default);
}

public class CatalogTokenProvider : AppTokenProvider, ICatalogTokenProvider
{
    public CatalogTokenProvider(
        CatalogConfig config,
        ITokenAcquisition tokenAcquisition,
        ILogger<CatalogTokenProvider> logger
    )
        : base(config.Scope, "catalog", tokenAcquisition, logger) { }
}
