using FakeConfluentGateway.App.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog();
builder.ConfigureDafda();

var app = builder.Build();
app.MapGet("ping", () => Results.Content("Pong!"));

app.Run();
