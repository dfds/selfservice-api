using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SelfService.Infrastructure.CopilotInsights;

/// <summary>
/// Result of a typed call. <c>Available=false</c> means copilot-insights could not answer
/// (transport error, 5xx, rejected credentials, unreadable body). An address that maps to no
/// GitHub login, or an unknown login, is <c>Available=true</c> with <c>Data=null</c> and the
/// upstream error code.
/// </summary>
public sealed record CopilotResult<T>(T? Data, bool Available, int StatusCode, CopilotMetaDto? Meta, string? ErrorCode)
    where T : class
{
    public static CopilotResult<T> Unavailable(int statusCode) => new(null, false, statusCode, null, null);
}

/// <summary>An admin response with its upstream status and body.</summary>
public sealed record CopilotProxyResponse(bool Available, int StatusCode, string? Body)
{
    public static CopilotProxyResponse Unavailable(int statusCode) => new(false, statusCode, null);
}

public interface ICopilotInsightsClient
{
    Task<CopilotResult<CopilotUserBundleDto>> GetUserByEmail(
        IReadOnlyList<string> emails,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    );

    /// An unknown login is <c>Available=true</c>, <c>Data=null</c>, error code <c>user_not_found</c>.
    Task<CopilotResult<CopilotUserBundleDto>> GetUserByLogin(
        string login,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    );

    /// Users whose login, name or any email contains <paramref name="q"/>, best matches first.
    Task<CopilotResult<List<CopilotIdentitySummaryDto>>> SearchUsers(
        string q,
        int limit,
        CancellationToken cancellationToken = default
    );

    /// <param name="path">Path below <c>/api/v1/</c>, already validated and escaped.</param>
    /// <param name="queryString">Raw query string including the leading "?", or empty.</param>
    Task<CopilotProxyResponse> ProxyGet(string path, string queryString, CancellationToken cancellationToken = default);
}

public class CopilotInsightsClient : ICopilotInsightsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // These errors mean a completed lookup, not an unavailable service.
    private static readonly HashSet<string> UnmappedErrorCodes = new()
    {
        "not_mapped",
        "no_dfds_email",
        "user_not_found",
    };

    private readonly HttpClient _httpClient;
    private readonly CopilotInsightsConfig _config;
    private readonly ICopilotInsightsTokenProvider _tokenProvider;
    private readonly ILogger<CopilotInsightsClient> _logger;

    public CopilotInsightsClient(
        HttpClient httpClient,
        CopilotInsightsConfig config,
        ICopilotInsightsTokenProvider tokenProvider,
        ILogger<CopilotInsightsClient> logger
    )
    {
        _httpClient = httpClient;
        _config = config;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public async Task<CopilotResult<CopilotUserBundleDto>> GetUserByEmail(
        IReadOnlyList<string> emails,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    )
    {
        var query = string.Join("&", emails.Select(e => "email=" + Uri.EscapeDataString(e)));
        return await GetBundle($"users/by-email?{query}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", cancellationToken);
    }

    public Task<CopilotResult<CopilotUserBundleDto>> GetUserByLogin(
        string login,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    )
    {
        return GetBundle(
            $"users/{Uri.EscapeDataString(login)}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken
        );
    }

    public async Task<CopilotResult<List<CopilotIdentitySummaryDto>>> SearchUsers(
        string q,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Send($"identity/users?q={Uri.EscapeDataString(q)}&limit={limit}", cancellationToken);
        if (response is null)
        {
            return CopilotResult<List<CopilotIdentitySummaryDto>>.Unavailable(0);
        }

        var (status, body) = response.Value;
        if (status != (int)HttpStatusCode.OK)
        {
            _logger.LogWarning("copilot-insights user search returned {StatusCode}", status);
            return CopilotResult<List<CopilotIdentitySummaryDto>>.Unavailable(status);
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<CopilotEnvelope<List<CopilotIdentitySummaryDto>>>(
                body,
                JsonOptions
            );
            return new CopilotResult<List<CopilotIdentitySummaryDto>>(
                envelope?.Data ?? new List<CopilotIdentitySummaryDto>(),
                true,
                status,
                envelope?.Meta,
                null
            );
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "copilot-insights returned an unreadable body with status {StatusCode}", status);
            return CopilotResult<List<CopilotIdentitySummaryDto>>.Unavailable(status);
        }
    }

    private async Task<CopilotResult<CopilotUserBundleDto>> GetBundle(
        string relative,
        CancellationToken cancellationToken
    )
    {
        var response = await Send(relative, cancellationToken);
        if (response is null)
        {
            return CopilotResult<CopilotUserBundleDto>.Unavailable(0);
        }

        var (status, body) = response.Value;
        try
        {
            if (status == (int)HttpStatusCode.OK)
            {
                var envelope = JsonSerializer.Deserialize<CopilotEnvelope<CopilotUserBundleDto>>(body, JsonOptions);
                if (envelope?.Data is null)
                {
                    _logger.LogWarning("copilot-insights returned an empty user bundle");
                    return CopilotResult<CopilotUserBundleDto>.Unavailable(status);
                }
                return new CopilotResult<CopilotUserBundleDto>(envelope.Data, true, status, envelope.Meta, null);
            }

            if (status is (int)HttpStatusCode.NotFound or (int)HttpStatusCode.BadRequest)
            {
                var error = JsonSerializer.Deserialize<CopilotErrorDto>(body, JsonOptions);
                if (error?.Error is { } code && UnmappedErrorCodes.Contains(code))
                {
                    return new CopilotResult<CopilotUserBundleDto>(null, true, status, null, code);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "copilot-insights returned an unreadable body with status {StatusCode}", status);
            return CopilotResult<CopilotUserBundleDto>.Unavailable(status);
        }

        _logger.LogWarning("copilot-insights user lookup returned {StatusCode}", status);
        return CopilotResult<CopilotUserBundleDto>.Unavailable(status);
    }

    public async Task<CopilotProxyResponse> ProxyGet(
        string path,
        string queryString,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Send(path + queryString, cancellationToken);
        if (response is null)
        {
            return CopilotProxyResponse.Unavailable(0);
        }

        var (status, body) = response.Value;

        // A 401/403 rejects the service token, not the portal user's session.
        if (status >= 500 || status is (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden)
        {
            // Only the first segment: deeper ones can hold a login or an email address.
            _logger.LogWarning("copilot-insights returned {StatusCode} for {Route}", status, path.Split('/', 2)[0]);
            return CopilotProxyResponse.Unavailable(status);
        }

        return new CopilotProxyResponse(true, status, body);
    }

    private async Task<(int Status, string Body)?> Send(string relative, CancellationToken cancellationToken)
    {
        if (_config.Endpoint is null)
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_config.Endpoint, "api/v1/" + relative));

            var token = await _tokenProvider.GetAccessToken(cancellationToken);
            if (token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return ((int)response.StatusCode, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reach copilot-insights at {Endpoint}", _config.Endpoint);
            return null;
        }
    }
}
