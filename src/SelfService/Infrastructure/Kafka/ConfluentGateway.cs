using System;
using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;

namespace SelfService.Infrastructure.Kafka;

public class ConfluentGateway : IConfluentGatewayService
{
    private readonly ILogger<ConfluentGateway> _logger;
    private readonly HttpClient _httpClient;

    public ConfluentGateway(ILogger<ConfluentGateway> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<List<KafkaSchema>> ListSchemas(KafkaSchemaQueryParams queryParams)
    {
        using var _ = _logger.BeginScope("{Action} on {ImplementationType}", nameof(ListSchemas), GetType().FullName);

        try
        {
            var queryString = "schemas";

            if (!string.IsNullOrWhiteSpace(queryParams.SubjectPrefix))
            {
                queryString += $"?subjectPrefix={queryParams.SubjectPrefix}";
            }

            Console.WriteLine("Listing schemas");
            Console.WriteLine($"Query string: {queryString}");
            using HttpResponseMessage response = await _httpClient.GetAsync($"{queryString}");

            response.EnsureSuccessStatusCode();
            Console.WriteLine("Schemas listed");
            Console.WriteLine(response.Content.ReadAsStringAsync().Result);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

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
