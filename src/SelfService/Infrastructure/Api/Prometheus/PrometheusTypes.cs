namespace SelfService.Infrastructure.Api.Prometheus;


public class Response
{
    //public string status;
    public Data data;
}

public class Data
{
    //public string resultType;
    public Result[] result;
}

public class Result
{
    public Metric metric;
    //public string[] value;
}

public class Metric
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
