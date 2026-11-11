using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SelfService.Infrastructure.CopilotInsights;

public static class CopilotUnmappedReason
{
    public const string ServicePrincipal = "service_principal";
    public const string NoDfdsEmail = "no_dfds_email";
    public const string NotMapped = "not_mapped";
    public const string UserNotFound = "user_not_found";
}

public interface ICopilotResponse
{
    // False when copilot-insights could not answer; such a response never replaces a cached one.
    bool Available { get; }

    // Cache entry size: the length of the upstream body.
    long Size { get; }
}

// Available=false means an upstream request failed. An unmapped user has
// Available=true, Data=null and an unmapped reason in ErrorCode.
public sealed record CopilotResult<T>(T? Data, bool Available, CopilotMetaDto? Meta = null, string? ErrorCode = null)
    : ICopilotResponse
    where T : class
{
    public static readonly CopilotResult<T> Unavailable = new(null, false);

    public long Size { get; init; }
}

public sealed record CopilotProxyResponse(bool Available, int StatusCode, string? Body) : ICopilotResponse
{
    public static CopilotProxyResponse Unavailable(int statusCode) => new(false, statusCode, null);

    public long Size => Body?.Length ?? 0;
}

public interface ICopilotInsightsClient
{
    Task<CopilotResult<CopilotUserBundleDto>> GetUserByEmail(
        IReadOnlyList<string> emails,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    );

    // An unknown login is Available=true, Data=null, error code user_not_found.
    Task<CopilotResult<CopilotUserBundleDto>> GetUserByLogin(
        string login,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    );

    // Users whose login, name or any email contains q, best matches first.
    Task<CopilotResult<List<CopilotIdentitySummaryDto>>> SearchUsers(
        string q,
        int limit,
        CancellationToken cancellationToken = default
    );

    // path is validated and escaped, relative to /api/v1/.
    // queryString includes the leading "?", or is empty.
    Task<CopilotProxyResponse> ProxyGet(string path, string queryString, CancellationToken cancellationToken = default);
}

public class CopilotInsightsClient : ICopilotInsightsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // These errors mean a completed lookup, not an unavailable service.
    private static readonly HashSet<string> UnmappedErrorCodes = new()
    {
        CopilotUnmappedReason.NotMapped,
        CopilotUnmappedReason.NoDfdsEmail,
        CopilotUnmappedReason.UserNotFound,
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

    public Task<CopilotResult<CopilotUserBundleDto>> GetUserByEmail(
        IReadOnlyList<string> emails,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    )
    {
        var query = string.Join("&", emails.Select(e => "email=" + Uri.EscapeDataString(e)));
        return GetEnvelope<CopilotUserBundleDto>(
            $"users/by-email?{query}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            null,
            cancellationToken
        );
    }

    public Task<CopilotResult<CopilotUserBundleDto>> GetUserByLogin(
        string login,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    )
    {
        return GetEnvelope<CopilotUserBundleDto>(
            $"users/{Uri.EscapeDataString(login)}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            null,
            cancellationToken
        );
    }

    public Task<CopilotResult<List<CopilotIdentitySummaryDto>>> SearchUsers(
        string q,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        // copilot-insights encodes an empty result as "data": null.
        return GetEnvelope(
            $"identity/users?q={Uri.EscapeDataString(q)}&limit={limit}",
            new List<CopilotIdentitySummaryDto>(),
            cancellationToken
        );
    }

    // whenNoData supplies a fallback for null data in a 200 response.
    // Without a fallback, treat the response as unavailable.
    private async Task<CopilotResult<T>> GetEnvelope<T>(
        string relative,
        T? whenNoData,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var response = await Send(relative, cancellationToken);
        if (response is null)
        {
            return CopilotResult<T>.Unavailable;
        }

        var (status, body) = response.Value;
        try
        {
            if (status == (int)HttpStatusCode.OK)
            {
                var envelope = JsonSerializer.Deserialize<CopilotEnvelope<T>>(body, JsonOptions);
                if ((envelope?.Data ?? whenNoData) is { } data)
                {
                    return new CopilotResult<T>(data, true, envelope?.Meta) { Size = body.Length };
                }
            }
            else if (status is (int)HttpStatusCode.NotFound or (int)HttpStatusCode.BadRequest)
            {
                var error = JsonSerializer.Deserialize<CopilotErrorDto>(body, JsonOptions);
                if (error?.Error is { } code && UnmappedErrorCodes.Contains(code))
                {
                    return new CopilotResult<T>(null, true, ErrorCode: code) { Size = body.Length };
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "copilot-insights returned an unreadable body with status {StatusCode}", status);
            return CopilotResult<T>.Unavailable;
        }

        _logger.LogWarning("copilot-insights returned {StatusCode} for {Route}", status, Route(relative));
        return CopilotResult<T>.Unavailable;
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

        // Only 2xx and request rejections are cacheable. Transient failures and token
        // rejections must not replace the last good response.
        if (status is not ((>= 200 and < 300) or (int)HttpStatusCode.BadRequest or (int)HttpStatusCode.NotFound))
        {
            _logger.LogWarning("copilot-insights returned {StatusCode} for {Route}", status, Route(path));
            return CopilotProxyResponse.Unavailable(status);
        }

        return new CopilotProxyResponse(true, status, body);
    }

    // Deeper segments may contain logins or email addresses that must not appear in logs.
    private static string Route(string relative) => relative.Split('/', 2)[0];

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
