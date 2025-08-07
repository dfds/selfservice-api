using System;
using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;

namespace SelfService.Infrastructure.Kafka;

public class ConfluentGatewayService : IConfluentGatewayService
{
    private readonly ILogger<ConfluentGatewayService> _logger;
    private readonly IKafkaClusterRepository _kafkaClusterRepository;
    private readonly HttpClient _httpClient;

    public ConfluentGatewayService(ILogger<ConfluentGatewayService> logger, IKafkaClusterRepository kafkaClusterRepository, HttpClient httpClient)
    {
        _logger = logger;
        _kafkaClusterRepository = kafkaClusterRepository;
        _httpClient = httpClient;
    }

    public async Task<List<KafkaSchema>> ListSchemas(string clusterId, KafkaSchemaQueryParams queryParams)
    {
        using var _ = _logger.BeginScope("{Action} on {ImplementationType}", nameof(ListSchemas), GetType().FullName);

        try
        {
            var queryString = $"clusters/{clusterId}/schemas";

            /* ignore subjectPrefix for now
            if (!string.IsNullOrWhiteSpace(queryParams.SubjectPrefix))
            {
                queryString += $"?subjectPrefix={queryParams.SubjectPrefix}";
            }
            */

            using HttpResponseMessage response = await _httpClient.GetAsync($"{queryString}");

            response.EnsureSuccessStatusCode();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var kafkaSchemas = await response.Content.ReadFromJsonAsync<List<KafkaSchema>>(options);

            return kafkaSchemas ?? new List<KafkaSchema>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list schemas");
            throw;
        }
    }

    public async Task<List<ConfluentTopic>> ListTopics(string clusterId)
    {
        using var _ = _logger.BeginScope("{Action} on {ImplementationType}", nameof(ListTopics), GetType().FullName);

        try
        {
            var queryString = $"clusters/{clusterId}/topics";

            using HttpResponseMessage response = await _httpClient.GetAsync($"{queryString}");

            response.EnsureSuccessStatusCode();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var kafkaTopics = await response.Content.ReadFromJsonAsync<List<ConfluentTopic>>(options);

            return kafkaTopics ?? new List<ConfluentTopic>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list topics");
            throw;
        }
    }
}
