using FakeConfluentGateway.App.Configuration;

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
    
    return Results.Ok(new {});
});

app.Run();
