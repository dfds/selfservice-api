using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FakePlatformDataApi.App;

Random random = new Random();

// ids gotten from db/seed/Capabilities.csv
const string capabilityIdA = "cool-beans-xxx";
const string capabilityIdB = "cloudengineering-xxx";


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var finoutResponseJson = CreateTimeSeriesFinoutJson();
var awsResourcesResponseJson = CreateAwsResourcesJson();

app.MapGet("ping", () => Results.Content("Pong!"));
app.MapGet(
    "api/data/timeseries/finout",
    () => { return Results.Content(finoutResponseJson); }
);
app.MapGet(
    "api/data/counts/aws-resources",
    () => Results.Content(awsResourcesResponseJson)
);
app.Run();

#region Finout

// Using values seen in production giving out of bounds charts in the MyCapabilities section
string CreateOutOfBoundsTimeSeriesFinoutJson()
{
    List<PlatformDataApiTimeSeries> timeSeries = new List<PlatformDataApiTimeSeries>();

    // ids gotten from db/seed/Capabilities.csv
    var capabilityIds = new string[] { capabilityIdA, capabilityIdB };

    var addAllCapabilitiesFunc = (float value, DateTime timeStamp) =>
    {
        foreach (var capabilityId in capabilityIds)
        {
            timeSeries.Add(
                new()
                {
                    Tag = capabilityId,
                    TimeStamp = timeStamp,
                    Value = value,
                }
            );
        }
    };

    var now = DateTime.UtcNow;
    var startMidnight = new DateTime(now.Year, now.Month, now.Day).AddDays(-30);
    var current = startMidnight;

    // Still need 30 days of data, but we are only interested in the last 7 days
    for (int i = 0; i <= 30 - 7; i++)
    {
        current = current.AddDays(1);
        addAllCapabilitiesFunc(4.82f, current);
    }

    addAllCapabilitiesFunc(4.82f, current.AddDays(1));
    addAllCapabilitiesFunc(4.82f, current.AddDays(2));
    addAllCapabilitiesFunc(11.45f, current.AddDays(3));
    addAllCapabilitiesFunc(10.77f, current.AddDays(4));
    addAllCapabilitiesFunc(8.72f, current.AddDays(5));
    addAllCapabilitiesFunc(4.95f, current.AddDays(6));
    addAllCapabilitiesFunc(4.93f, current.AddDays(7));

    return JsonSerializer.Serialize(timeSeries.ToArray());
}

string CreateTimeSeriesFinoutJson()
{
    var easing = (float t) => t * t * t; // Make more exaggerated changes
    List<PlatformDataApiTimeSeries> timeSeries = new List<PlatformDataApiTimeSeries>();


    var randMinMax = (float minimum, float maximum) => (float)random.NextDouble() * (maximum - minimum) + minimum;

    var now = DateTime.UtcNow;
    var startMidnight = new DateTime(now.Year, now.Month, now.Day).AddDays(-30);

    var current = startMidnight;
    var lowCostsSequence = 1f;
    var lowFluctuation = lowCostsSequence / 4f;
    var highCostsSequence = 1000f;
    var highFluctuation = highCostsSequence / 4f;
    for (int i = 0; i <= 30; i++)
    {
        current = current.AddDays(1);
        timeSeries.Add(
            new()
            {
                Tag = capabilityIdA,
                TimeStamp = current,
                Value = lowCostsSequence
            }
        );
        lowCostsSequence += easing((float)random.NextDouble()) * randMinMax(-lowFluctuation, lowFluctuation);
        if (lowCostsSequence <= 0)
        {
            lowCostsSequence = 0.5f;
        }

        timeSeries.Add(
            new()
            {
                Tag = capabilityIdB,
                TimeStamp = current,
                Value = highCostsSequence
            }
        );
        highCostsSequence += easing((float)random.NextDouble()) * randMinMax(-highFluctuation, highFluctuation);
    }

    return JsonSerializer.Serialize(timeSeries.ToArray());
}

#endregion

#region AwsResouces

string CreateAwsResourcesJson()
{
    var counts = new Dictionary<string, List<PlatformDataApiAwsResourceCount>>();
    var lines = File.ReadLines("example-resource-counts.csv");
    string pattern = "\"{accountid=([0-9]*)}\"";
    Regex rg = new Regex(pattern, RegexOptions.IgnoreCase);
    foreach (var line in lines)
    {
        var splitted = line.Split(',');
        var resourceId = splitted[0].Replace("\"", "");

        var matches = rg.Match(splitted[1]).Groups;
        var awsId = matches[1].Value; // aws account id for capabilityIdB
        var countString = splitted[2].Replace("\"", "");
        var count = countString == "" ? 0 : (int)float.Parse(countString);
        if (!counts.ContainsKey(awsId))
        {
            counts.Add(awsId, new List<PlatformDataApiAwsResourceCount>());
        }

        counts[awsId].Add(new PlatformDataApiAwsResourceCount()
        {
            ResourceId = resourceId,
            Count = count,
        });
    }

    var awsResourceCounts = new List<PlatformDataApiAwsResourceCounts>();
    foreach (var (awsId, awsCounts) in counts)
    {
        awsResourceCounts.Add(new PlatformDataApiAwsResourceCounts()
        {
            AwsAccountId = awsId,
            Counts = awsCounts.ToArray(),
        });
    }

    return JsonSerializer.Serialize(awsResourceCounts.ToArray());
}

#endregion