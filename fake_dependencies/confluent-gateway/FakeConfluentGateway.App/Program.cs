using FakeConfluentGateway.App.Configuration;

Random random = new Random();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog();
builder.ConfigureDafda();

var app = builder.Build();
app.MapGet("ping", () => Results.Content("Pong!"));
app.MapPost("sendnotification", async (HttpContext context) =>
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
});

app.Run();

string RandomAccountId(int length)
{
    const string chars = "0123456789";
    return new string(Enumerable.Repeat(chars, length)
        .Select(s => s[random.Next(s.Length)]).ToArray());
}