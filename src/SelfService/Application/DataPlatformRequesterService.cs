using System.Text;
using System.Text.Json;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Application;

public class DataPlatformRequesterService : IDataPlatformRequesterService
{
    private struct DataPlatformApiTimeSeries
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

    public async Task<CapabilityCosts> GetCapabilityCosts(CapabilityId capabilityId, int daysWindow)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"{_httpClient.BaseAddress}/api/data/timeseries/finout");
        sb.Append($"?tag={capabilityId}");
        if (daysWindow > 0)
        {
            sb.Append($"&days-window={daysWindow}");
        }

        HttpResponseMessage response = await _httpClient.GetAsync(sb.ToString());

        if (!response.IsSuccessStatusCode)
            throw new DataPlatformApiUnavailableException($"DataPlatformApi StatusCode: {response.StatusCode}");

        string json = await response.Content.ReadAsStringAsync();
        _logger.LogDebug("Response was {StatusCode} with body {ResponseBody}", response.StatusCode, json);

        var dataResponse = JsonSerializer.Deserialize<DataPlatformApiTimeSeries[]>(json);
        if (dataResponse == null)
            return new CapabilityCosts(capabilityId, new TimeSeries[] { });

        List<TimeSeries> timeSeries = new List<TimeSeries>();

        foreach (var dataPlatformApiTimeSeries in dataResponse)
        {
            if (dataPlatformApiTimeSeries.Tag != capabilityId)
                continue;
            timeSeries.Add(new TimeSeries(dataPlatformApiTimeSeries.Value, dataPlatformApiTimeSeries.TimeStamp));
        }

        return new CapabilityCosts(capabilityId, timeSeries.ToArray());
    }
}
