using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.Api.Capabilities;

public static class Module
{
    public static void MapCapabilityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/capabilities").WithTags("Capability");

        group.MapGet("", GetAllCapabilities);
        group.MapGet("{id:required}", GetCapability).WithName("capability");
        group.MapGet("{id:required}/members", GetCapabilityMembers).WithName("capability members");
        group.MapGet("{id:required}/topics", GetCapabilityTopics).WithName("capability topics");
        group.MapPost("{id:required}/topics", AddCapabilityTopic).WithName("add capability topic"); // TODO [jandr@2023-02-22]: consider moving this to topics "controller"

        group.MapGet("{id:required}/awsaccount", GetCapabilityAwsAccount).WithName("capability aws account");
    }

    private static object ConvertToResourceDto(Capability capability, HttpContext httpContext, LinkGenerator linkGenerator)
    {
        return new
        {
            id = capability.Id,
            name = capability.Name,
            description = capability.Description,
            _links = new
            {
                self = new
                {
                    href = linkGenerator.GetUriByName(httpContext, "capability", new {id = capability.Id}),
                    rel = "self",
                    allow = new[] {"GET"}
                },
                members = new
                {
                    href = linkGenerator.GetUriByName(httpContext, "capability members", new {id = capability.Id}),
                    rel = "related",
                    allow = new[] {"GET"}
                },
                topics = new
                {
                    href = linkGenerator.GetUriByName(httpContext, "capability topics", new {id = capability.Id}),
                    rel = "related",
                    allow = new[] {"GET"}
                },
            }
        };
    }

    private static async Task<IResult> GetCapability(string id, SelfServiceDbContext dbContext, HttpContext context, LinkGenerator linkGenerator)
    {
        var capability = await dbContext
            .Capabilities
            .SingleOrDefaultAsync(x => x.Id == id);

        if (capability is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ConvertToResourceDto(capability, context, linkGenerator));
    }

    private static async Task<IResult> GetAllCapabilities(HttpContext httpContext, SelfServiceDbContext dbContext,
        LinkGenerator linkGenerator)
    {
        var capabilities = await dbContext
            .Capabilities
            .Where(c => c.Deleted == null)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Results.Ok(new
        {
            items = capabilities
                .Select(x => ConvertToResourceDto(x, httpContext, linkGenerator))
                .ToArray()
        });
    }

    private static async Task<IResult> GetCapabilityMembers(string id, ICapabilityMembersQuery membersQuery)
    {
        var errors = new Dictionary<string, string[]>();
        if (!CapabilityId.TryParse(id, out var capabilityId))
        {
            errors.Add(nameof(id), new [] {$"Value \"{id}\" is not a valid capability id."});
        }

        if (errors.Any())
        {
            return Results.ValidationProblem(errors);
        }

        var members = await membersQuery.FindBy(capabilityId);

        return Results.Ok(new
        {
            items = members
                .Select(x => new
                {
                    upn = x.Id.ToString(),
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
                    retention = x
                        .Retention, // TODO [jandr@2023-02-10]: convert to days (that's the units for the frontend),
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
                            href = linkGenerator.GetUriByName(httpContext,
                                "kafkaclusters"), // TODO [jandr@2023-02-10]: this returns null!
                            rel = "related",
                            allow = new[] {"GET"}
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

    private static async Task<IResult> GetCapabilityAwsAccount(HttpContext context, string id, SelfServiceDbContext dbContext)
    {
        var errors = new Dictionary<string, string[]>();

        if (!CapabilityId.TryParse(id, out var capabilityId))
        {
            errors.Add(nameof(id), new[] { $"Value \"{id}\" is not a valid capability id." });
        }

        if (errors.Any())
        {
            return Results.ValidationProblem(errors);
        }

        var account = await dbContext.AwsAccounts.SingleOrDefaultAsync(x => x.CapabilityId == capabilityId);
        if (account is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new
        {
            Id = account.Id.ToString(),
            AwsAccountId = account.AccountId.ToString(),
            RoleArn = account.RoleArn.ToString(),
            RoleEmail = account.RoleEmail,
            CreatedAt = account.CreatedAt.ToString("O"),
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
            items = capabilities.Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    description = x.Description,
                    _links = new
                    {
                        self = new
                        {
                            href = linkGenerator.GetUriByName(httpContext, "capability", new {id = x.Id}),
                            rel = "self",
                            allow = new[] {"GET"}
                        },
                        members = new
                        {
                            href = "",
                            rel = "related",
                            allow = new[] {"GET"}
                        },
                        topics = new
                        {
                            href = "",
                            rel = "related",
                            allow = new[] {"GET"}
                        },
                    }
                })
                .ToArray()
        });
    }
}