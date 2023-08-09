using System.Net.Http;
using System.Text.Json;
using SelfService.Domain.Exceptions;
using SelfService.Application;

namespace SelfService.Infrastructure.Api.Prometheus;

public class PrometheusClient : IKafkaTopicConsumerService
{
    private readonly ILogger<PrometheusClient> _logger;
    private readonly HttpClient _httpClient;

    public PrometheusClient(ILogger<PrometheusClient> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    private IEnumerable<string> GetConsumersFromResponse(Response? response, string topic)
    {
        List<string> consumers = new List<string>();
        if (response == null || response.data == null || response.data.result == null)
        {
            return new List<string>();
        }
        foreach (Result result in response.data.result)
        {
            if (result.metric == null || result.metric.topic == null || result.metric.consumergroup == null)
            {
                continue;
            }
            if (result.metric.topic == topic)
            {
                consumers.Add(result.metric.consumergroup);
            }
        }
        return consumers;
    }

    public async Task<IEnumerable<string>> GetConsumersForKafkaTopic(string topic)
    {
        string url = $"{_httpClient.BaseAddress}/api/v1/query?query=kafka_consumergroup_lag"; // consider time parameter (e.g "&time=1689844553.339")
        HttpResponseMessage response = await _httpClient.GetAsync(url);

        string jsonstring = await response.Content.ReadAsStringAsync();
        _logger.LogDebug("Response was {StatusCode} with body {ResponseBody}", response.StatusCode, jsonstring);

        if (response.IsSuccessStatusCode)
        {
            if (jsonstring == null)
            {
                throw new KafkaTopicConsumersUnavailable($"Prometheus response is null");
            }
            Response? promResponse = JsonSerializer.Deserialize<Response>(jsonstring);
            return (GetConsumersFromResponse(promResponse, topic));
        }
        else
        {
            throw new KafkaTopicConsumersUnavailable($"Prometheus StatusCode: {response.StatusCode}");
        }
    }
}
