using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Persistence;
using SelfService.Infrastructure.Persistence.Queries;

namespace SelfService.Application;

public class PlatformDataApiRequesterService : IPlatformDataApiRequesterService
{
    #region ResponseObjects

    private class PlatformDataApiTimeSeries
    {
        [JsonPropertyName("timestamp")]
        public DateTime TimeStamp { get; set; }

        [JsonPropertyName("value")]
        public float Value { get; set; }

        [JsonPropertyName("tag")]
        public string Tag { get; set; } = "";
    }

    private class PlatformDataApiAwsResourceCount
    {
        [JsonPropertyName("resource_id")]
        public string ResourceId { get; set; } = "";

        [JsonPropertyName("count")]
        public int Count { get; set; } = 0;
    }

    private class PlatformDataApiAwsResourceCounts
    {
        [JsonPropertyName("aws_account_id")]
        public string AwsAccountId { get; set; } = "";

        [JsonPropertyName("counts")]
        public PlatformDataApiAwsResourceCount[] Counts { get; set; } = Array.Empty<PlatformDataApiAwsResourceCount>();
    }

    #endregion

    private string GetTimeSeriesUrl(Uri? baseUrl)
    {
        return $"{baseUrl}api/data/timeseries/finout";
    }

    private string GetResourceCountsUrl(Uri? baseUrl)
    {
        return $"{baseUrl}api/data/counts/aws-resources";
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
    private const int DaysToFetch = 30;

    private readonly ILogger<PlatformDataApiRequesterService> _logger;
    private readonly IAwsAccountRepository _awsAccountRepository;
    private readonly IMyCapabilitiesQuery _myCapabilitiesQuery;
    private readonly HttpClient _httpClient;

    public PlatformDataApiRequesterService(
        ILogger<PlatformDataApiRequesterService> logger,
        IAwsAccountRepository awsAccountRepository,
        IMyCapabilitiesQuery myCapabilitiesQuery,
        HttpClient httpClient
    )
    {
        _logger = logger;
        _awsAccountRepository = awsAccountRepository;
        _myCapabilitiesQuery = myCapabilitiesQuery;
        _httpClient = httpClient;
    }

    private async Task<List<PlatformDataApiTimeSeries>> FetchCapabilityCosts(int daysWindow)
    {
        var queryParams = new KeyValuePair<string, string>[] { new(QueryParamDaysWindow, daysWindow.ToString()) };
        var url = ConstructUrl(GetTimeSeriesUrl(_httpClient.BaseAddress), queryParams);
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

    private async Task<List<PlatformDataApiAwsResourceCounts>> FetchAwsResourceCounts()
    {
        var queryParams = new KeyValuePair<string, string>[] { };
        var url = ConstructUrl(GetResourceCountsUrl(_httpClient.BaseAddress), queryParams);
        HttpResponseMessage response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new PlatformDataApiUnavailableException($"PlatformDataApi StatusCode: {response.StatusCode}");

        string json = await response.Content.ReadAsStringAsync();
        _logger.LogTrace("[PlatformDataApiRequesterService] Response was {StatusCode}", response.StatusCode);
        List<PlatformDataApiAwsResourceCounts> validSeries = new List<PlatformDataApiAwsResourceCounts>();
        var dataResponse = JsonSerializer.Deserialize<PlatformDataApiAwsResourceCounts[]>(json);

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

    private Dictionary<string, List<PlatformDataApiAwsResourceCount>> ToAwsAccountMap(
        List<PlatformDataApiAwsResourceCounts> counts
    )
    {
        Dictionary<string, List<PlatformDataApiAwsResourceCount>> costs =
            new Dictionary<string, List<PlatformDataApiAwsResourceCount>>();
        foreach (var count in counts)
        {
            if (!costs.ContainsKey(count.AwsAccountId))
            {
                costs.Add(count.AwsAccountId, new List<PlatformDataApiAwsResourceCount>());
            }

            costs[count.AwsAccountId].AddRange(count.Counts);
        }

        return costs;
    }

    public async Task<MyCapabilitiesMetrics> GetMyCapabilitiesMetrics(UserId userId)
    {
        var myCapabilities = await _myCapabilitiesQuery.FindBy(userId);
        var capabilitiesArray = myCapabilities as Capability[] ?? myCapabilities.ToArray();
        if (capabilitiesArray.Length == 0)
            return new EmptyMyCapabilitiesMetrics();

        var awsAccountIdToCapabilityIdMap = new Dictionary<string, CapabilityId>();
        foreach (var myCapability in capabilitiesArray)
        {
            var awsAccount = await _awsAccountRepository.FindBy(myCapability.Id);
            if (awsAccount is null)
                continue;
            awsAccountIdToCapabilityIdMap.Add(awsAccount.Id, myCapability.Id);
        }

        var timeSeriesWithValidCapabilities = await FetchCapabilityCosts(DaysToFetch);
        var mappedCosts = ToCapabilityMap(timeSeriesWithValidCapabilities);

        List<CapabilityCosts> costs = new List<CapabilityCosts>();
        foreach (var myCapability in capabilitiesArray)
        {
            if (mappedCosts.TryGetValue(myCapability.Id, out var myCosts))
                costs.Add(new CapabilityCosts(myCapability.Id, myCosts.ToArray()));
        }

        var awsResourceCounts = await FetchAwsResourceCounts();

        var mappedCounts = ToAwsAccountMap(awsResourceCounts);

        List<CapabilityAwsResourceCounts> resourceCounts = new List<CapabilityAwsResourceCounts>();
        foreach (var accountsCount in awsResourceCounts)
        {
            if (!awsAccountIdToCapabilityIdMap.TryGetValue(accountsCount.AwsAccountId, out var capabilityId))
            {
                // not an error, remember my capabilities are a subset of all capabilities
                continue;
            }

            if (!mappedCounts.TryGetValue(accountsCount.AwsAccountId, out var counts))
            {
                _logger.LogError(
                    "unable to find expected resource counts for aws account with id {AwsAccountId}",
                    accountsCount.AwsAccountId
                );
                continue;
            }

            List<AwsResourceCount> convertedCounts = new List<AwsResourceCount>();
            foreach (var resourceCount in counts)
            {
                convertedCounts.Add(new AwsResourceCount(resourceCount.ResourceId, resourceCount.Count));
            }

            resourceCounts.Add(new CapabilityAwsResourceCounts(capabilityId, convertedCounts));
        }

        return new MyCapabilitiesMetrics(costs, resourceCounts);
    }
}
