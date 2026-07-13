using System.Net.Http.Headers;
using System.Text.Json;

namespace SelfService.Infrastructure.Catalog;

public interface ICatalogClient
{
    Task<CatalogSnapshotDto?> GetCatalog(Uri clusterUrl, CancellationToken cancellationToken = default);
}

public class CatalogClient : ICatalogClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly ICatalogTokenProvider _tokenProvider;
    private readonly ILogger<CatalogClient> _logger;

    public CatalogClient(HttpClient httpClient, ICatalogTokenProvider tokenProvider, ILogger<CatalogClient> logger)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public async Task<CatalogSnapshotDto?> GetCatalog(Uri clusterUrl, CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri(clusterUrl, "/api/v1/catalog");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            var token = await _tokenProvider.GetAccessToken(cancellationToken);
            if (token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ssu-catalog at {ClusterUrl} returned {StatusCode}; skipping this cluster",
                    clusterUrl,
                    response.StatusCode
                );
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var envelope = await JsonSerializer.DeserializeAsync<CatalogEnvelope<CatalogSnapshotDto>>(
                stream,
                JsonOptions,
                cancellationToken
            );

            return envelope?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch catalog from {ClusterUrl}; skipping this cluster", clusterUrl);
            return null;
        }
    }
}
