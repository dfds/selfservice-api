using System.Text.Json;
using System.Text.Json.Serialization;
using FakeConfluentGateway.App.Configuration;


Random random = new Random();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog();
builder.ConfigureDafda();

var finoutResponseJson = CreateTimeSeriesFinoutJson();

var app = builder.Build();
app.MapGet("ping", () => Results.Content("Pong!"));
app.MapGet("api/data/timeseries/finout",
    () => { return Results.Content(finoutResponseJson); }
);
app.MapPost(
    "sendnotification",
    async (HttpContext context) =>
    {
        // print received request
        var request = context.Request;
        var headers = request.Headers;
        using var sr = new StreamReader(request.Body);
        var content = await sr.ReadToEndAsync();
        Console.WriteLine("-----------------------------------------------------------");
        Console.WriteLine($"{request.Method + " " + request.Path} {request.Protocol}");
        foreach (var (key, value) in headers)
        {
            Console.WriteLine($"{key}: {value}");
        }

        Console.WriteLine();
        Console.WriteLine(content);
        Console.WriteLine("-----------------------------------------------------------");

        // send messages

        var legacyProducer = context.RequestServices.GetRequiredService<LegacyProducer>();
        var contextId = headers["CONTEXT_ID"];
        var accountId = RandomAccountId(12);
        var capabilityRootId = headers["CAPABILITY_ROOT_ID"];
        var capabilityName = headers["CAPABILITY_NAME"];
        var contextName = headers["CONTEXT_NAME"];
        var capabilityId = headers["CAPABILITY_ID"];

        _ = Task.Run(async () =>
        {
            {
                await Task.Delay(2000);

                var message = new AwsContextAccountCreated
                {
                    ContextId = contextId,
                    AccountId = accountId,
                    RoleArn = $"arn:aws:iam::{accountId}:root",
                    RoleEmail = "role@aws.com",
                    CapabilityRootId = capabilityRootId,
                    CapabilityName = capabilityName,
                    ContextName = contextName,
                    CapabilityId = capabilityId,
                };

                await legacyProducer.SendAwsContextAccountCreated(message);
            }

            {
                await Task.Delay(5000);

                var message = new K8sNamespaceCreatedAndAwsArnConnected
                {
                    CapabilityId = capabilityId,
                    ContextId = contextId,
                    NamespaceName = capabilityId
                };

                await legacyProducer.SendK8sNamespaceCreatedAndAwsArnConnected(message);
            }
        });

        return Results.Ok();
    }
);

app.Run();

string RandomAccountId(int length)
{
    const string chars = "0123456789";
    return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
}

string CreateTimeSeriesFinoutJson()
{
    var easing = (float t) => t * t * t; // Make more exaggerated changes
    List<PlatformDataApiTimeSeries> timeSeries = new List<PlatformDataApiTimeSeries>();

// ids gotten from db/seed/Capabilities.csv
    const string lowCostCapabilityId = "pending-deletion-xxx";
    const string highCostCapabilityId = "cloudengineering-xxx";
    
    var randMinMax =(float minimum,float maximum) => (float)random.NextDouble() * (maximum - minimum) + minimum;

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
        timeSeries.Add(new()
            { Tag = lowCostCapabilityId, TimeStamp = current, Value = lowCostsSequence });
        lowCostsSequence += easing((float)random.NextDouble()) * randMinMax(-lowFluctuation, lowFluctuation);
        if (lowCostsSequence <= 0)
        {
            lowCostsSequence = 0.5f;
        }
        timeSeries.Add(new()
            { Tag = highCostCapabilityId, TimeStamp = current, Value = highCostsSequence });
        highCostsSequence += easing((float)random.NextDouble()) * randMinMax(-highFluctuation, highFluctuation);
    }

    return JsonSerializer.Serialize(timeSeries.ToArray());
}

class PlatformDataApiTimeSeries
{
    [JsonPropertyName("timestamp")] public DateTime TimeStamp { get; set; }

    [JsonPropertyName("value")] public float Value { get; set; }

    [JsonPropertyName("tag")] public string Tag { get; set; } = "";
}