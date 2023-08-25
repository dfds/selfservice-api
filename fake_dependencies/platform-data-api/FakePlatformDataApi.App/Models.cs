using System.Text.Json.Serialization;

namespace FakePlatformDataApi.App;


class PlatformDataApiTimeSeries
{
    [JsonPropertyName("timestamp")] public DateTime TimeStamp { get; set; }

    [JsonPropertyName("value")] public float Value { get; set; }

    [JsonPropertyName("tag")] public string Tag { get; set; } = "";
}

class PlatformDataApiAwsResourceCount
{
    [JsonPropertyName("resource_id")] public string ResourceId { get; set; } = "";

    [JsonPropertyName("count")] public int Count { get; set; } = 0;
}

class PlatformDataApiAwsResourceCounts
{
    [JsonPropertyName("aws_account_id")] public string AwsAccountId { get; set; } = "";

    [JsonPropertyName("counts")]
    public PlatformDataApiAwsResourceCount[] Counts { get; set; } = Array.Empty<PlatformDataApiAwsResourceCount>();
}