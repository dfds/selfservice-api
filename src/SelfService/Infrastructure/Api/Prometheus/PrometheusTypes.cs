namespace SelfService.Infrastructure.Api.Prometheus;


class Response
{
    //public string status;
    public Data data;
}

class Data
{
    //public string resultType;
    public Result[] result;
}

class Result
{
    public Metric metric;
    //public string[] value;
}

class Metric
{
    //public string __name__;
    public string consumergroup;
    //public string endpoint;
    //public string instance;
    //public string job;
    //public string namespace;
    //public string partition;
    //public string pod;
    //public string service;
    public string topic;
}
