namespace SelfService.Infrastructure.Api.Prometheus;

/*
 * Contains only the types declarations needed for listing consumers.
 * Remaining types exists as comments for ease of reference.
 */

public record Response
{
    //public string status;
    public Data data;
}

public record Data
{
    //public string resultType;
    public Result[] result;
}

public record Result
{
    public Metric metric;
    //public string[] value;
}

public record Metric
{
    //public string __name__;
    public string consumergroup;
    //public string endpoint;
    //public string instance;
    //public string job;
    //public string namespace;
    public string partition;
    //public string pod;
    //public string service;
    public string topic;
}
