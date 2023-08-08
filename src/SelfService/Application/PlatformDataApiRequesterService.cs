using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Persistence;
using SelfService.Infrastructure.Persistence.Queries;

namespace SelfService.Application;

public class PlatformDataApiRequesterService : IPlatformDataApiRequesterService
{
    private class PlatformDataApiTimeSeries
    {
        [JsonPropertyName("timestamp")]
        public DateTime TimeStamp { get; set; }

        [JsonPropertyName("value")]
        public float Value { get; set; }

        [JsonPropertyName("tag")]
        public string Tag { get; set; } = "";
    }

    private string GetTimeSeriesUrl()
    {
        return $"{_httpClient.BaseAddress}api/data/timeseries/finout";
    }

    private string GetTimeSeriesByGroupUrl()
    {
        return $"{_httpClient.BaseAddress}api/data/timeseriesbygroup/finout";
    }

    private string ConstructUrl(string url, params KeyValuePair<string, string>[] args)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(url);
        if (args.Length > 0)
        {
            sb.Append('?');
        }

        for (int i = 0; i < args.Length; i++)
        {
            var (key, value) = args[i];
            sb.Append($"{key}={value}");
            if (i < args.Length - 1)
            {
                sb.Append("&");
            }
        }

        return sb.ToString();
    }

    private const string QueryParamDaysWindow = "days-window";
    private const string QueryParamCapabilityId = "tag";

    private readonly ILogger<PlatformDataApiRequesterService> _logger;
    private readonly IMyCapabilitiesQuery _myCapabilitiesQuery;
    private readonly HttpClient _httpClient;

    public PlatformDataApiRequesterService(
        ILogger<PlatformDataApiRequesterService> logger,
        IMyCapabilitiesQuery myCapabilitiesQuery,
        HttpClient httpClient
    )
    {
        _logger = logger;
        _myCapabilitiesQuery = myCapabilitiesQuery;
        _httpClient = httpClient;
    }

    private async Task<List<PlatformDataApiTimeSeries>> FetchCapabilityCosts(
        params KeyValuePair<string, string>[] queryParams
    )
    {
        var url = ConstructUrl(GetTimeSeriesUrl(), queryParams);
        HttpResponseMessage response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new PlatformDataApiUnavailableException($"PlatformDataApi StatusCode: {response.StatusCode}");

        string json = await response.Content.ReadAsStringAsync();
        _logger.LogTrace("[PlatformDataApiRequesterService] Response was {StatusCode}", response.StatusCode);
        List<PlatformDataApiTimeSeries> validSeries = new List<PlatformDataApiTimeSeries>();
        var dataResponse = JsonSerializer.Deserialize<PlatformDataApiTimeSeries[]>(json);

        if (dataResponse != null)
        {
            validSeries.AddRange(dataResponse);
        }

        return validSeries;
    }

    private Dictionary<string, List<TimeSeries>> ToCapabilityMap(List<PlatformDataApiTimeSeries> validTimeSeries)
    {
        Dictionary<string, List<TimeSeries>> costs = new Dictionary<string, List<TimeSeries>>();
        foreach (var series in validTimeSeries)
        {
            if (!costs.ContainsKey(series.Tag))
            {
                costs.Add(series.Tag, new List<TimeSeries>());
            }

            costs[series.Tag].Add(new TimeSeries(series.Value, series.TimeStamp));
        }

        return costs;
    }

    public async Task<MyCapabilityCosts> GetMyCapabilityCosts(UserId userId, int daysWindow)
    {
        var timeSeriesWithValidCapabilities = await FetchCapabilityCosts(
            new KeyValuePair<string, string>[] { new(QueryParamDaysWindow, daysWindow.ToString()) }
        );

        var mappedCosts = ToCapabilityMap(timeSeriesWithValidCapabilities);

        var myCapabilities = await _myCapabilitiesQuery.FindBy(userId);
        List<CapabilityCosts> costs = new List<CapabilityCosts>();
        foreach (var myCapability in myCapabilities)
        {
            if (mappedCosts.TryGetValue(myCapability.Id, out var myCosts))
                costs.Add(new CapabilityCosts(myCapability.Id, myCosts.ToArray()));
        }

        return new MyCapabilityCosts(costs);
    }

    public async Task<CapabilityCosts> GetCapabilityCosts(CapabilityId capabilityId, int daysWindow)
    {
        var timeSeriesWithValidCapabilities = await FetchCapabilityCosts(
            new(QueryParamCapabilityId, capabilityId),
            new(QueryParamDaysWindow, daysWindow.ToString())
        );

        var mappedCosts = ToCapabilityMap(timeSeriesWithValidCapabilities);
        return mappedCosts.TryGetValue(capabilityId, out var cost)
            ? new CapabilityCosts(capabilityId, cost.ToArray())
            : new CapabilityCosts(capabilityId, new TimeSeries[] { });
    }
}
