using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.Api.Capabilities;

public static class Module
{
    public static void MapCapabilityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/capabilities").WithTags("Capability");

        group.MapGet("", GetCapabilityList);
        group.MapGet("{id:required}", GetCapability).WithName("capability");
        group.MapGet("{id:required}/members", GetCapabilityMembers).WithName("capability members");
        group.MapGet("{id:required}/topics", GetCapabilityTopics).WithName("capability topics");
        group.MapPost("{id:required}/topics", AddCapabilityTopic).WithName("add capability topic");

        //group.MapPost("", NotImplemented);
        //group.MapPut("{id:guid}", NotImplemented);
        //group.MapDelete("{id:guid}", NotImplemented);
        //group.MapPost("{id:guid}/members", NotImplemented);
        //group.MapDelete("{id:guid}/members/{memberEmail}", NotImplemented);
        //group.MapPost("{id:guid}/contexts", NotImplemented);
    }

    private static async Task<IResult> GetCapabilityList(HttpContext httpContext, SelfServiceDbContext dbContext, LinkGenerator linkGenerator)
    {
        var capabilities = await dbContext
            .Capabilities
            .Where(c => c.Deleted == null)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Results.Ok(new
        {
            items = capabilities
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    description = x.Description,
                    _links = new
                    {
                        self = new
                        {
                            href = linkGenerator.GetUriByName(httpContext, "capability", new { id = x.Id }),
                            rel = "self",
                            allow = new[]{ "GET" }
                        },
                        members = new
                        {
                            href = linkGenerator.GetUriByName(httpContext, "capability members", new { id = x.Id }),
                            rel = "related",
                            allow = new[] { "GET" }
                        },
                        topics = new
                        {
                            href = linkGenerator.GetUriByName(httpContext, "capability topics", new { id = x.Id }),
                            rel = "related",
                            allow = new[] { "GET" }
                        },
                    }
                })
                .ToArray()
        });
    }

    private static async Task<IResult> GetCapabilityMembers(string id, HttpContext httpContext, SelfServiceDbContext dbContext, LinkGenerator linkGenerator)
    {
        var capability = await dbContext
            .Capabilities
            .Include(x => x.Memberships)
            .ThenInclude(x => x.Member)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (capability == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new
        {
            items = capability
                .Memberships
                .Select(x => x.Member)
                .Select(x => new
                {
                    upn = x.UPN,
                    name = x.DisplayName,
                    email = x.Email,
                })
                .ToArray()
        });
    }

    private static async Task<IResult> GetCapabilityTopics(string id, HttpContext httpContext, SelfServiceDbContext dbContext, LinkGenerator linkGenerator)
    {
        var topics = await dbContext
            .KafkaTopics
            .Where(x => x.CapabilityId == id)
            .ToListAsync();

        var clusters = await dbContext
            .KafkaClusters
            .Where(x => x.Enabled)
            .ToListAsync();

        return Results.Ok(new
        {
            items = topics
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    description = x.Description,
                    kafkaClusterId = x.KafkaClusterId,
                    partitions = x.Partitions,
                    retention = x.Retention, // TODO [jandr@2023-02-10]: convert to days (that's the units for the frontend),
                    status = x.Status,
                })
                .ToArray(),
            _embedded = new
            {
                kafkaClusters = new
                {
                    items = clusters
                        .Select(x => new
                        {
                            id = x.Id,
                            name = x.Name,
                            description = x.Description,
                        })
                        .ToArray(),
                    _links = new
                    {
                        self = new
                        {
                            href = linkGenerator.GetUriByName(httpContext, "kafkaclusters"), // TODO [jandr@2023-02-10]: this returns null!
                            rel = "related",
                            allow = new[] { "GET" }
                        }
                    }
                }
            },
            _links = new
            {
                self = new
                {
                    href = linkGenerator.GetUriByName(httpContext, "capability topics", new {id = id}),
                    rel = "self",
                    allow = new[] {"GET"}
                }
            }
        });
    }

    private static async Task<IResult> AddCapabilityTopic(HttpContext httpContext, SelfServiceDbContext dbContext, LinkGenerator linkGenerator)
    {
        var capabilities = await dbContext
            .Capabilities
            .Where(c => c.Deleted == null)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Results.Ok(new
        {
            items = capabilities
                //.Select(CapabilityListItemDto.Create)
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    description = x.Description,
                    _links = new
                    {
                        self = new
                        {
                            href = linkGenerator.GetUriByName(httpContext, "capability", new { id = x.Id }),
                            rel = "self",
                            allow = new[]{ "GET" }
                        },
                        members = new
                        {
                            href = "",
                            rel = "related",
                            allow = new[] { "GET" }
                        },
                        topics = new
                        {
                            href = "",
                            rel = "related",
                            allow = new[] { "GET" }
                        },
                    }
                })
                .ToArray()
        });
    }

    private static async Task<IResult> GetCapability(string id, SelfServiceDbContext context)
    {
        var capability = await context
            .Capabilities
            .Include(x => x.Memberships)
            .ThenInclude(x => x.Member)
            .Include(x => x.AwsAccount)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (capability == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(CapabilityDetailDto.Create(capability));
    }

    private static IResult NotImplemented()
    {
        return Results.StatusCode(StatusCodes.Status501NotImplemented);
    }
}