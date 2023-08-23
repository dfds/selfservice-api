using System.Text.Json;
using System.Text.Json.Serialization;

Random random = new Random();
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var finoutResponseJson = CreateTimeSeriesFinoutJson();

app.MapGet(
    "api/data/timeseries/finout",
    () =>
    {
        return Results.Content(finoutResponseJson);
    }
);
app.Run();


// Using values seen in production giving out of bounds charts in the MyCapabilities section
string CreateOutOfBoundsTimeSeriesFinoutJson()
{
    List<PlatformDataApiTimeSeries> timeSeries = new List<PlatformDataApiTimeSeries>();

    // ids gotten from db/seed/Capabilities.csv
    var capabilityIds = new string[] { "pending-deletion-xxx", "cloudengineering-xxx" };

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

    // ids gotten from db/seed/Capabilities.csv
    const string lowCostCapabilityId = "pending-deletion-xxx";
    const string highCostCapabilityId = "cloudengineering-xxx";

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
                Tag = lowCostCapabilityId,
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
                Tag = highCostCapabilityId,
                TimeStamp = current,
                Value = highCostsSequence
            }
        );
        highCostsSequence += easing((float)random.NextDouble()) * randMinMax(-highFluctuation, highFluctuation);
    }

    return JsonSerializer.Serialize(timeSeries.ToArray());
}

class PlatformDataApiTimeSeries
{
    [JsonPropertyName("timestamp")]
    public DateTime TimeStamp { get; set; }

    [JsonPropertyName("value")]
    public float Value { get; set; }

    [JsonPropertyName("tag")]
    public string Tag { get; set; } = "";
}
