using SelfService.Domain.Models;

namespace SelfService.Application;

public class DataPlatformRequesterService : IDataPlatformRequesterService
{
    struct DataPlatformApiTimeSeries
    {
        public DateTime TimeStamp { get; set; }
        public float Value { get; set; }
        public string Tag { get; set; }
    }

    private readonly ILogger<DataPlatformRequesterService> _logger;
    private readonly HttpClient _httpClient;

    public DataPlatformRequesterService(ILogger<DataPlatformRequesterService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    private DataPlatformApiTimeSeries[] GetAsyncWithParameters(int daysWindow)
    {
        
    }

    public Task<CapabilityCosts> GetCapabilityCosts(string capabilityId, int daysWindow)
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
        else {
            throw new KafkaTopicConsumersUnavailable($"Prometheus StatusCode: {response.StatusCode}");
        }   
    }
}