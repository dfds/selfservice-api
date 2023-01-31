using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public static class Projections
{
    public static async Task<IResult> GetMe(LegacyDbContext dbContext, ClaimsPrincipal user, LinkGenerator linkGenerator, HttpContext httpContext)
    {
        var name = user.Identity.Name;
        
        var capabilities = await dbContext
            .Capabilities
            .Where(x => x.Memberships.Any(y => y.Email.ToLower()==name.ToLower()))
            .Where(c => c.Deleted == null)
            .Include(x => x.Memberships)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return TypedResults.Ok(new Me
        {
            Capabilities = capabilities
                .Select(x => new MyCapability
                {
                    Id = x.Id.ToString(),
                    RootId = x.RootId,
                    Name = x.Name,
                    Description = x.Description,
                    Members = x.Memberships.Select(x => x.Email).ToArray(),
                    Links = new Link[]
                    {
                        new Link("self", linkGenerator.GetUriByName(httpContext, "capability", new { id = x.Id}))
                    }
                })
                .ToArray()
        });
    }
    
    
    public class Me
    {
        public MyCapability[] Capabilities { get; set; }
    }
    
    public class MyCapability
    {
        public string Id { get; set; }
        public string RootId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Members { get; set; }
        public Link[] Links { get; set; } = Array.Empty<Link>();
    }
    
    public record Link(string rel, string href);


    public static async Task<IResult> GetCapabilityList(LegacyDbContext dbContext)
    {
        var capabilities = await dbContext
            .Capabilities
            .Where(c => c.Deleted == null)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Results.Ok(new { items = capabilities.Select(CapabilityListItemDto.Create).ToArray() });
    }

    public static async Task<IResult> GetCapability(Guid id, LegacyDbContext dbContext)
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
        public static MemberDto Create(Membership member)
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

    public static async Task<IResult> GetClusterList(LegacyDbContext dbContext)
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

    public static async Task<IResult> GetAllByCapability(Guid id, LegacyDbContext dbContext)
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

    public static async Task<IResult> GetAllTopics(LegacyDbContext dbContext)
    {
        var topics = await dbContext
            .Topics
            .ToListAsync();

        return Results.Ok(new
        {
            Items = topics.Select(TopicDto.CreateFrom).ToArray()
        });
    }

    public record TopicDto(Guid Id, string? Name, string? Description, Guid CapabilityId, Guid KafkaClusterId, int Partitions, string? Status)
    {
        public Dictionary<string, object>? Configurations { get; set; }

        public static TopicDto CreateFrom(Topic topic)
        {
            var topicDto = new TopicDto(
                topic.Id,
                topic.Name,
                topic.Description,
                topic.CapabilityId,
                topic.KafkaClusterId,
                topic.Partitions,
                topic.Status)
            {
                Configurations = topic.Configurations
            };

            return topicDto;
        }
    }

    public static async Task<IResult> GetServiceCatalog(SelfServiceDbContext dbContext)
    {
        var serviceDescriptions = await dbContext.ServiceCatalog
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Results.Ok(new
        {
            items = serviceDescriptions.Select(x => new ServiceDescriptionDto(x.Id, x.Name)).ToArray()
        });
    }

    public record ServiceDescriptionDto(Guid Id, string Name);

    public static async Task<IResult> GetService(Guid id, SelfServiceDbContext dbContext)
    {
        var serviceDescription = await dbContext.ServiceCatalog.FirstOrDefaultAsync();
        if (serviceDescription == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(serviceDescription);
    }
}