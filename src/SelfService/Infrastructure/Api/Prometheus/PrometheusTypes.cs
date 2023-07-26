using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Prometheus;


/*
 * Contains only the types declarations needed for listing consumers.
 * Remaining types exists as comments for ease of reference.
 */

public class Response
{
    [JsonPropertyName("status")]
    public string status { get; set; }
    [JsonPropertyName("data")]
    public Data data { get; set; }
}

public class Data
{
    //public string resultType;
    [JsonPropertyName("result")]
    public Result[] result { get; set; }
}

public class Result
{
    [JsonPropertyName("metric")]
    public Metric metric { get; set; }
    //public string[] value;
}

public class Metric
{
    //public string __name__;
    [JsonPropertyName("consumergroup")]
    public string consumergroup { get; set; }
    //public string endpoint;
    //public string instance;
    //public string job;
    //public string namespace;
    [JsonPropertyName("partition")]
    public string partition { get; set; }
    //public string pod;
    //public string service;
    [JsonPropertyName("topic")]
    public string topic { get; set; }
}
