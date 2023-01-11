using Microsoft.EntityFrameworkCore;
using SelfService.Domain;

namespace SelfService.Infrastructure.Persistence;

public static class Projections
{
    public static IResult NotImplemented(object? id, object? memberEmail, object? name)
    {
        return Results.StatusCode(StatusCodes.Status501NotImplemented);
    }

    public static async Task<IResult> GetCapabilityList(SelfServiceDbContext dbContext)
    {
        var capabilities = await dbContext
            .Capabilities
            .Where(c => c.Deleted == null)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Results.Ok(new { items = capabilities.Select(CapabilityListItemDto.Create).ToArray() });
    }

    public static async Task<IResult> GetCapability(Guid id, SelfServiceDbContext dbContext)
    {
        var capability = await dbContext
            .Capabilities
            .Include(x => x.Memberships)
            .Include(x => x.Contexts)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (capability == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(CapabilityDetailDto.Create(capability));
    }

    public record CapabilityListItemDto(Guid Id, string? Name, string? RootId, string? Description)
    {
        public static CapabilityListItemDto Create(Capability capability)
        {
            return new CapabilityListItemDto(
                capability.Id,
                capability.Name,
                capability.RootId,
                capability.Description
            );
        }
    }

    public class CapabilityDetailDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? RootId { get; set; }
        public string? Description { get; set; }
        public MemberDto[] Members { get; set; } = Array.Empty<MemberDto>();
        public ContextDto[] Contexts { get; set; } = Array.Empty<ContextDto>();

        public static CapabilityDetailDto Create(Capability capability)
        {
            return new CapabilityDetailDto
            {
                Id = capability.Id,
                Name = capability.Name,
                RootId = capability.RootId,
                Description = capability.Description,
                Members = capability.Memberships
                    .Select(x => x.Member!)
                    .Select(MemberDto.Create)
                    .ToArray(),
                Contexts = capability
                    .Contexts
                    .Select(ContextDto.Create)
                    .ToArray(),
            };
        }
    }

    public record MemberDto(string Email)
    {
        public static MemberDto Create(Member member)
        {
            return new MemberDto(member.Email);
        }
    }

    public record ContextDto(Guid Id, string? Name, string? AWSAccountId, string? AWSRoleArn, string? AWSRoleEmail)
    {
        public static ContextDto Create(Context context)
        {
            return new ContextDto(
                context.Id,
                context.Name,
                context.AWSAccountId,
                context.AWSRoleArn,
                context.AWSRoleEmail
            );
        }
    }

    public static async Task<IResult> GetClusterList(SelfServiceDbContext dbContext)
    {
        var clusters = await dbContext.Clusters.ToListAsync();

        return Results.Ok(clusters.Select(ClusterDto.Create));
    }

    private record ClusterDto(string? Name, string? Description, bool Enabled, string? ClusterId)
    {
        public static ClusterDto Create(Cluster cluster)
        {
            return new ClusterDto(cluster.Name, cluster.Description, cluster.Enabled, cluster.ClusterId);
        }
    }
    
    public static async Task<IResult> GetAllByCapability(Guid id, SelfServiceDbContext dbContext)
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

    public static async Task<IResult> GetAllTopics(SelfServiceDbContext dbContext)
    {
        var topics =await dbContext
            .Topics
            .ToListAsync();

        return Results.Ok(new
        {
            Items = topics.Select(TopicDto.CreateFrom).ToArray()
        });
    }

    public record TopicDto(Guid Id, string? Name, string? Description, Guid CapabilityId, Guid KafkaClusterId, int Partitions, string? Status)
    {    
        public Dictionary<string, object> Configurations { get; set; }

        public static TopicDto CreateFrom(Topic topic)
        {
            var topicDto = new TopicDto(
                topic.Id,
                topic.Name,
                topic.Description,
                topic.CapabilityId,
                topic.KafkaClusterId,
                topic.Partitions,
                topic.Status
            )
            {
                Configurations = topic.Configurations
            };

            return topicDto;
        }
    }

    
}