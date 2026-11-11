using Microsoft.Identity.Web;

namespace SelfService.Infrastructure;

// Uses selfservice-api's service principal for downstream tokens. An empty scope permits
// local development without OIDC. Acquisition errors are logged and return null, leaving
// the downstream service to reject the unauthenticated request.
public abstract class AppTokenProvider
{
    private readonly string _scope;
    private readonly string _downstream;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly ILogger _logger;

    protected AppTokenProvider(string scope, string downstream, ITokenAcquisition tokenAcquisition, ILogger logger)
    {
        _scope = scope;
        _downstream = downstream;
        _tokenAcquisition = tokenAcquisition;
        _logger = logger;
    }

    public async Task<string?> GetAccessToken(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_scope))
        {
            return null;
        }

        try
        {
            return await _tokenAcquisition.GetAccessTokenForAppAsync(_scope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire {Downstream} access token for scope {Scope}", _downstream, _scope);
            return null;
        }
    }
}
