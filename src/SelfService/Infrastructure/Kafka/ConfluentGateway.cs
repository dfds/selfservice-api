using System;
using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;

namespace SelfService.Infrastructure.Kafka;

public class ConfluentGatewayService : IConfluentGatewayService
{
    private readonly ILogger<ConfluentGatewayService> _logger;
    private readonly HttpClient _httpClient;

    public ConfluentGatewayService(ILogger<ConfluentGatewayService> logger, HttpClient httpClient)
    {
        _logger = logger;
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
}
