using FakeConfluentGateway.App.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog();
builder.ConfigureDafda();

var app = builder.Build();
app.MapGet("ping", () => Results.Content("Pong!"));
app.MapPost("sendnotification", async (HttpContext context) =>
{
    using var sr = new StreamReader(context.Request.Body);
    var content = await sr.ReadToEndAsync();
    
    Console.WriteLine("Received:\n" + content);
    
    return Results.Ok(new {});
});

app.Run();
