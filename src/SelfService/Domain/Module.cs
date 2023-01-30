using SelfService.Infrastructure.Api.Configuration;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Domain;

public static class Module
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => "Hello World!").WithTags("System").NoSwaggerDocs();

        app.MapGet("/me", Projections.GetMe).WithTags("Account").Produces<Projections.Me>();
        
        MapCapabilityEndpoints(app);

        MapClusterEndpoints(app);

        MapKafkaTopicEndpoints(app);

        MapServiceCatalogEndpoints(app);
    }

    private static void MapCapabilityEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/capabilities").WithTags("Capability");

        group.MapGet("", Projections.GetCapabilityList);
        group.MapGet("{id:guid}", Projections.GetCapability).WithName("capability");
        group.MapPost("", NotImplemented);
        group.MapPut("{id:guid}", NotImplemented);
        group.MapDelete("{id:guid}", NotImplemented);
        group.MapPost("{id:guid}/members", NotImplemented);
        group.MapDelete("{id:guid}/members/{memberEmail}", NotImplemented);
        group.MapPost("{id:guid}/contexts", NotImplemented);
    }

    private static void MapClusterEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/kafka/cluster").WithTags("Cluster");
        group.MapGet("", Projections.GetClusterList);
    }

    private static void MapKafkaTopicEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").WithTags("KafkaTopics");
        group.MapGet("/capabilities/{id:guid}/topics", Projections.GetAllByCapability);
        group.MapGet("/capabilities/{id:guid}/request-credential-generation", NotImplemented);
        group.MapGet("/topics", Projections.GetAllTopics);
        group.MapPost("/capabilities/{id:guid}/topics", NotImplemented);
        group.MapDelete("/topics/{name}", NotImplemented);
    }

    private static IResult NotImplemented()
    {
        return Results.StatusCode(StatusCodes.Status501NotImplemented);
    }

    private static void MapServiceCatalogEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/apispecs").WithTags("ServiceCatalog");

        group.MapGet("", Projections.GetServiceCatalog).AllowAnonymous();
        group.MapGet("{id:guid}", Projections.GetService).AllowAnonymous();
    }
}