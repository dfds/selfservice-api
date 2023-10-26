using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public class ApiKey
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class ListApiKeyData { }

public class ListApiKeysResponse
{
    public required List<ListApiKeyData> Data { get; set; }
}

public class ConfluentCloudClientService : IConfluentCloudClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ConfluentCloudClientService> _logger;

    public ConfluentCloudClientService(HttpClient httpClient, ILogger<ConfluentCloudClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    private string? BuildUrl(string url)
    {
        return _httpClient.BaseAddress + url;
    }

    private HttpContent BuildContent(object payload)
    {
        string jsonPayload = JsonSerializer.Serialize(payload);
        return new StringContent(jsonPayload, Encoding.UTF8, "application/json");
    }

    public Task<string> CreateServiceAccount(string name, string description)
    {
        throw new NotImplementedException();
    }

    public async Task<ListApiKeysResponse?> ListApiKeys(string serviceAccountId, string resourceId)
    {
        return await _httpClient.GetFromJsonAsync<ListApiKeysResponse>(
            BuildUrl($"/iam/v2/api-keys?spec.owner={serviceAccountId}&spec.resource={resourceId}")
        );
    }

    public async Task<ApiKey?> CreateApiKey(string serviceAccountId, string resourceId, string description)
    {
        var payload = new
        {
            spec = new
            {
                display_name = $"{resourceId}{serviceAccountId}",
                description = "Created with Confluent Gateway",
                owner = new { id = serviceAccountId },
                resource = new { id = resourceId }
            }
        };
        var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl("/iam/v2/api-keys"));
        request.Content = BuildContent(payload);
        string? apiKey = Environment.GetEnvironmentVariable("CONFLUENT_CLOUD_ADMIN_API_KEY");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", apiKey);

        var resp = await _httpClient.SendAsync(request);

        if (resp.IsSuccessStatusCode)
        {
            return await resp.Content.ReadFromJsonAsync<ApiKey>();
        }

        _logger.LogError("Error creating api key: {StatusCode}", resp.StatusCode);

        return null;
    }

    public void DeleteApiKey(string apikeyId)
    {
        throw new NotImplementedException();
    }

    public void CreateServiceAccountRoleBinding(string serviceAccountId, string resourceId, string roleName)
    {
        throw new NotImplementedException();
    }

    public void CreateTopic(KafkaClusterId clusterId, string topicName, int partitions, long retention)
    {
        throw new NotImplementedException();
    }

    public void DeleteTopic(KafkaClusterId clusterId, string topicName)
    {
        throw new NotImplementedException();
    }

    public void RegisterTopicSchema(KafkaClusterId clusterId, string topicName, string schema)
    {
        throw new NotImplementedException();
    }

    public void DeleteTopicSchema(KafkaClusterId clusterId, string topicName, string version)
    {
        throw new NotImplementedException();
    }

    public async Task CreateAclEntries(KafkaClusterId clusterId, CreateAclRequestData[] aclEntries)
    {
        await _httpClient.PostAsync(
            BuildUrl($"kafka/v3/clusters/${clusterId}/acls:batch"),
            BuildContent(aclEntries.ToArray())
        );
    }
}
