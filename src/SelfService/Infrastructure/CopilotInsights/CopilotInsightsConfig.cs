namespace SelfService.Infrastructure.CopilotInsights;

public sealed class CopilotInsightsConfig
{
    /// Base URL of copilot-insights, always ending in "/". Null when unset or not an absolute URL.
    public Uri? Endpoint { get; }
    public string Scope { get; }

    public bool Enabled => Endpoint is not null;
    public bool TokenAcquisitionEnabled => !string.IsNullOrWhiteSpace(Scope);

    public CopilotInsightsConfig(string? endpoint, string? scope)
    {
        var raw = endpoint?.Trim() ?? "";
        if (raw.Length > 0 && !raw.EndsWith('/'))
        {
            raw += "/";
        }

        Endpoint = Uri.TryCreate(raw, UriKind.Absolute, out var uri) ? uri : null;
        Scope = scope ?? "";
    }
}
