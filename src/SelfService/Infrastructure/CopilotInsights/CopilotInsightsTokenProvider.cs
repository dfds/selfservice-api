using Microsoft.Identity.Web;

namespace SelfService.Infrastructure.CopilotInsights;

public interface ICopilotInsightsTokenProvider
{
    Task<string?> GetAccessToken(CancellationToken cancellationToken = default);
}

public class CopilotInsightsTokenProvider : AppTokenProvider, ICopilotInsightsTokenProvider
{
    public CopilotInsightsTokenProvider(
        CopilotInsightsConfig config,
        ITokenAcquisition tokenAcquisition,
        ILogger<CopilotInsightsTokenProvider> logger
    )
        : base(config.Scope, "copilot-insights", tokenAcquisition, logger) { }
}
