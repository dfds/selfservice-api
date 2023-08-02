using System.Text;
using System.Text.Json;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Application;

public class ApiPlatformDataApiRequesterService : IPlatformDataApiRequesterService
{
    public struct PlatformDataApiTimeSeries
    {
        public DateTime TimeStamp { get; set; }
        public float Value { get; set; }
        public string Tag { get; set; }
    }

    private string GetTimeSeriesUrl()
    {
        return $"{_httpClient.BaseAddress}/api/data/timeseries/finout";
    }

    private string GetTimeSeriesByGroupUrl()
    {
        return $"{_httpClient.BaseAddress}/api/data/timeseriesbygroup/finout";
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
            sb.Append($"?{key}={value}");
            if (i < args.Length - 1)
            {
                sb.Append("&");
            }
        }

        return sb.ToString();
    }

    private const string QueryParamDaysWindow = "days-window";
    private const string QueryParamCapabilityId = "tag";

    private readonly ILogger<ApiPlatformDataApiRequesterService> _logger;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly HttpClient _httpClient;

    public ApiPlatformDataApiRequesterService(
        ILogger<ApiPlatformDataApiRequesterService> logger,
        HttpClient httpClient,
        ICapabilityRepository capabilityRepository
    )
    {
        _logger = logger;
        _httpClient = httpClient;
        _capabilityRepository = capabilityRepository;
    }

    private async Task<List<PlatformDataApiTimeSeries>> FetchAndFilterValidCapabilities(
        params KeyValuePair<string, string>[] queryParams
    )
    {
        List<PlatformDataApiTimeSeries> validTimeSeries = new List<PlatformDataApiTimeSeries>();
        var url = ConstructUrl(GetTimeSeriesUrl(), queryParams);
        HttpResponseMessage response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new PlatformDataApiUnavailableException($"PlatformDataApi StatusCode: {response.StatusCode}");

        string json = await response.Content.ReadAsStringAsync();
        _logger.LogDebug("Response was {StatusCode} with body {ResponseBody}", response.StatusCode, json);

        var dataResponse = JsonSerializer.Deserialize<PlatformDataApiTimeSeries[]>(json);
        if (dataResponse == null)
            return validTimeSeries;

        Dictionary<string, bool> isCapabilityIdValid = new Dictionary<string, bool>();
        foreach (var platformDataApiTimeSeries in dataResponse)
        {
            var timeSeriesCapabilityId = platformDataApiTimeSeries.Tag;
            if (!isCapabilityIdValid.ContainsKey(timeSeriesCapabilityId))
            {
                var existsInRepo = await _capabilityRepository.Exists(timeSeriesCapabilityId);
                isCapabilityIdValid.Add(timeSeriesCapabilityId, existsInRepo);
            }

            if (isCapabilityIdValid[timeSeriesCapabilityId])
                validTimeSeries.Add(platformDataApiTimeSeries);
        }

        return validTimeSeries;
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

    public async Task<List<CapabilityCosts>> GetAllCapabilityCosts(int daysWindow)
    {
        var timeSeriesWithValidCapabilities = await FetchAndFilterValidCapabilities(
            new KeyValuePair<string, string>[] { new(QueryParamDaysWindow, daysWindow.ToString()) }
        );

        var mappedCosts = ToCapabilityMap(timeSeriesWithValidCapabilities);
        List<CapabilityCosts> costs = new List<CapabilityCosts>();
        foreach (var (tag, timeSeriesData) in mappedCosts)
        {
            if (!CapabilityId.TryParse(tag, out var capabilityId))
            {
                _logger.LogDebug("unable to parse tag to capability: {CapabilityId}", tag);
                continue;
            }

            costs.Add(new CapabilityCosts(capabilityId, timeSeriesData.ToArray()));
        }

        return costs;
    }

    public async Task<CapabilityCosts> GetCapabilityCosts(CapabilityId capabilityId, int daysWindow)
    {
        var timeSeriesWithValidCapabilities = await FetchAndFilterValidCapabilities(
            new(QueryParamCapabilityId, capabilityId),
            new(QueryParamDaysWindow, daysWindow.ToString())
        );

        var mappedCosts = ToCapabilityMap(timeSeriesWithValidCapabilities);
        return mappedCosts.TryGetValue(capabilityId, out var cost)
            ? new CapabilityCosts(capabilityId, cost.ToArray())
            : new CapabilityCosts(capabilityId, new TimeSeries[] { });
    }
}
