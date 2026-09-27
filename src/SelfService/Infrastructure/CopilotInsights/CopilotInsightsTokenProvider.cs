using Microsoft.Identity.Web;

namespace SelfService.Infrastructure.CopilotInsights;

public interface ICopilotInsightsTokenProvider
{
    Task<string?> GetAccessToken(CancellationToken cancellationToken = default);
}

public class CopilotInsightsTokenProvider : ICopilotInsightsTokenProvider
{
    private readonly CopilotInsightsConfig _config;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly ILogger<CopilotInsightsTokenProvider> _logger;

    public CopilotInsightsTokenProvider(
        CopilotInsightsConfig config,
        ITokenAcquisition tokenAcquisition,
        ILogger<CopilotInsightsTokenProvider> logger
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
            return null;
        }

        try
        {
            return await _tokenAcquisition.GetAccessTokenForAppAsync(_config.Scope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire copilot-insights access token for scope {Scope}", _config.Scope);
            return null;
        }
    }
}
