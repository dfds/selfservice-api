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
        var group = app.MapGroup("/kafkaclusters").WithTags("Cluster");
        group.MapGet("", GetAllClusters);
    }

    private static void MapKafkaTopicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").WithTags("KafkaTopics");
        group.MapGet("/capabilities/{id:guid}/request-credential-generation", NotImplemented);
        group.MapGet("/kafkatopics", GetAllTopics);
        group.MapDelete("/kafkatopics/{name}", NotImplemented);
    }

    private static async Task<IResult> GetAllClusters(SelfServiceDbContext context)
    {
        var clusters = await context.KafkaClusters
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Results.Ok(new
        {
            Items = clusters
                .Select(x => new
                {
                    Id = x.Id.ToString(),
                    Name = x.Name,
                    Description = x.Description,
                    Enabled = x.Enabled,
                    ClusterId = x.RealClusterId
                })
                .ToArray()
            // TODO [jandr@2023-02-23]: add links section
        });
    }

    private static async Task<IResult> GetAllTopics(SelfServiceDbContext dbContext)
    {
        var topics = await dbContext.KafkaTopics
            .OrderBy(x => x.KafkaClusterId)
            .ThenBy(x => x.Name)
            .ToListAsync();

        return Results.Ok(new
        {
            Items = topics
                .Select(topic => new {
                    Id = topic.Id.ToString(),
                    Name = topic.Name.ToString(),
                    Description = topic.Description,
                    CapabilityId = topic.CapabilityId.ToString(),
                    KafkaClusterId = topic.KafkaClusterId.ToString(),
                    Partitions = topic.Partitions,
                    Retention = topic.Retention,
                    Status = topic.Status switch
                    {
                        KafkaTopicStatusType.Requested => "Requested",
                        KafkaTopicStatusType.InProgress => "In Progress",
                        KafkaTopicStatusType.Provisioned => "Provisioned",
                        _ => "Unknown"
                    }
                })
                .ToArray()
        });
    }

    private static IResult NotImplemented()
    {
        return Results.StatusCode(StatusCodes.Status501NotImplemented);
    }
}