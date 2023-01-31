using Microsoft.EntityFrameworkCore;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.Api.Kafka;

public static class Module
{
    public static void MapKafkaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapClusterEndpoints();
        app.MapKafkaTopicEndpoints();
    }

    private static void MapClusterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/kafka/cluster").WithTags("Cluster");
        group.MapGet("", GetClusterList);
    }

    private static void MapKafkaTopicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").WithTags("KafkaTopics");
        group.MapGet("/capabilities/{id:guid}/topics", GetAllByCapability);
        group.MapGet("/capabilities/{id:guid}/request-credential-generation", NotImplemented);
        group.MapGet("/topics", GetAllTopics);
        group.MapPost("/capabilities/{id:guid}/topics", NotImplemented);
        group.MapDelete("/topics/{name}", NotImplemented);
    }

    private static async Task<IResult> GetClusterList(LegacyDbContext dbContext)
    {
        var clusters = await dbContext.Clusters.ToListAsync();

        return Results.Ok(clusters.Select(ClusterDto.Create));
    }

    private static async Task<IResult> GetAllByCapability(Guid id, LegacyDbContext dbContext)
    {
        var topics = await dbContext
            .Topics
            .Where(x => x.CapabilityId == id)
            .ToListAsync();

        return Results.Ok(new
        {
            Items = topics.Select(TopicDto.CreateFrom).ToArray()
        });
    }

    private static async Task<IResult> GetAllTopics(LegacyDbContext dbContext)
    {
        var topics = await dbContext
            .Topics
            .ToListAsync();

        return Results.Ok(new
        {
            Items = topics.Select(TopicDto.CreateFrom).ToArray()
        });
    }

    private static IResult NotImplemented()
    {
        return Results.StatusCode(StatusCodes.Status501NotImplemented);
    }
}