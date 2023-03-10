using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelfService.Application;
using SelfService.Domain.Exceptions;
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
        group.MapGet("{id:required}/membershipapplications", GetCapabilityMembershipApplications).WithName("capability membership applications");
        group.MapGet("{id:required}/topics", GetCapabilityTopics).WithName("capability topics");
        group.MapPost("{id:required}/topics", AddCapabilityTopic).WithName("add capability topic"); // TODO [jandr@2023-02-22]: consider moving this to topics "controller"
        group.MapGet("{id:required}/topics/{topicId:required}", GetCapabilityTopic).WithName("get capability topic"); // TODO [jandr@2023-02-22]: consider moving this to topics "controller"

        group.MapGet("{id:required}/awsaccount", GetCapabilityAwsAccount).WithName("capability aws account");
    }

    private static object ConvertToResourceDto(Capability capability, HttpContext httpContext, LinkGenerator linkGenerator)
    {
        return new
        {
            id = capability.Id.ToString(),
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

    private static async Task<IResult> GetAllCapabilities(HttpContext httpContext, [FromServices] SelfServiceDbContext dbContext,
        [FromServices] LinkGenerator linkGenerator)
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

    private static async Task<IResult> GetCapabilityMembers(string id, [FromServices] ICapabilityMembersQuery membersQuery)
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

    private static async Task<IResult> GetCapabilityMembershipApplications(string id, ClaimsPrincipal user, SelfServiceDbContext dbContext)
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

        var applications = await dbContext.MembershipApplications
            .Where(x => x.CapabilityId == capabilityId)
            .OrderBy(x => x.SubmittedAt)
            .ToListAsync();

        return Results.Ok(new
        {
            items = applications
                .Select(x => new
                {
                    id = x.Id.ToString(),
                    applicant = x.Applicant,
                    submittedAt = x.SubmittedAt,
                    deadlineAt = x.ExpiresOn,
                    approvedBy = x.Approvals
                        .Select(y => y.ApprovedBy)
                        .ToArray()
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
                    id = x.Id.ToString(),
                    name = x.Name.ToString(),
                    description = x.Description,
                    kafkaClusterId = x.KafkaClusterId.ToString(),
                    partitions = x.Partitions.ToString(),
                    retention = x.Retention.ToString(),
                    status = x.Status switch
                    {
                        KafkaTopicStatusType.Requested => "Requested",
                        KafkaTopicStatusType.InProgress => "In Progress",
                        KafkaTopicStatusType.Provisioned => "Provisioned",
                        _ => "Unknown"
                    }
                })
                .ToArray(),
            _embedded = new
            {
                kafkaClusters = new
                {
                    items = clusters
                        .Select(x => new
                        {
                            id = x.Id.ToString(),
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

    private static async Task<IResult> GetCapabilityTopic(string id, string topicId, HttpContext httpContext, 
        SelfServiceDbContext dbContext, LinkGenerator linkGenerator)
    {
        var errors = new Dictionary<string, string[]>();

        if (!CapabilityId.TryParse(id, out var capabilityId))
        {
            errors.Add(nameof(id), new[] { $"Value \"{id}\" is not a valid capability id." });
        }

        if (!KafkaTopicId.TryParse(topicId, out var kafkaTopicId))
        {
            errors.Add(nameof(topicId), new[] { $"Value \"{topicId}\" is not a valid kafka topic id." });
        }

        if (errors.Any())
        {
            return Results.ValidationProblem(errors);
        }

        var topic = await dbContext
            .KafkaTopics
            .SingleOrDefaultAsync(x => x.Id == kafkaTopicId && x.CapabilityId == capabilityId);

        if (topic is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new
        {
            id = topic.Id.ToString(),
            name = topic.Name.ToString(),
            description = topic.Description,
            kafkaClusterId = topic.KafkaClusterId.ToString(),
            partitions = topic.Partitions.ToString(),
            retention = topic.Retention.ToString(),
            status = topic.Status switch
            {
                KafkaTopicStatusType.Requested => "Requested",
                KafkaTopicStatusType.InProgress => "In Progress",
                KafkaTopicStatusType.Provisioned => "Provisioned",
                _ => "Unknown"
            },
            _links = new
            {
                self = new
                {
                    href = linkGenerator.GetUriByName(httpContext, "get capability topic", new {id = id, topicId = topic.Id}),
                    rel = "self",
                    allow = new[] {"GET"}
                }
            }
        });
    }

    private static async Task<IResult> AddCapabilityTopic(string id, [FromBody] NewKafkaTopicRequest topicRequest, 
        ClaimsPrincipal user, ICapabilityApplicationService capabilityApplicationService, IKafkaTopicRepository topicRepository, IKafkaClusterRepository clusterRepository)
    {
        var errors = new Dictionary<string, string[]>();

        if (!CapabilityId.TryParse(id, out var capabilityId))
        {
            errors.Add(nameof(id), new[] { $"Value \"{id}\" is not a valid capability id." });
        }

        if (!KafkaClusterId.TryParse(topicRequest.KafkaClusterId, out var kafkaClusterId))
        {
            errors.Add(nameof(topicRequest.KafkaClusterId), new[] { $"Value \"{topicRequest.KafkaClusterId}\" is not a valid kafka cluster id." });
        }

        if (!KafkaTopicName.TryParse(topicRequest.KafkaTopicName, out var kafkaTopicName))
        {
            errors.Add(nameof(topicRequest.KafkaTopicName), new[] { $"Value \"{topicRequest.KafkaTopicName}\" is not a valid kafka topic name." });
        }

        if (!KafkaTopicPartitions.TryCreate(topicRequest.Partitions ?? 0, out var topicPartitions))
        {
            errors.Add(nameof(topicRequest.Partitions), new[] { $"Value \"{topicRequest.Partitions}\" is invalid for kafka topic partitions." });
        }

        if (!KafkaTopicRetention.TryParse(topicRequest.Retention, out var topicRetention))
        {
            errors.Add(nameof(topicRequest.Retention), new[] { $"Value \"{topicRequest.Retention}\" is invalid for kafka topic retention." });
        }
        
        var upn = user.Identity?.Name?.ToLower();
        if (!UserId.TryParse(upn, out var userId))
        {
            errors.Add("upn", new[] { $"Identity \"{upn}\" is not a valid user id." });
        }

        if (!await clusterRepository.Exists(kafkaClusterId))
        {
            errors.Add(nameof(topicRequest.KafkaClusterId), new[] { $"Kafka cluster with id \"{kafkaClusterId}\" is unknown to the system." });
        }

        if (errors.Any())
        {
            return Results.ValidationProblem(errors);
        }

        try
        {
            var topicId = await capabilityApplicationService.RequestNewTopic(
                capabilityId: capabilityId,
                kafkaClusterId: kafkaClusterId,
                name: kafkaTopicName,
                description: topicRequest.Description ?? "",
                partitions: topicPartitions, 
                retention: topicRetention, 
                requestedBy: userId
            );

            var topic = await topicRepository.Get(topicId);

            return Results.CreatedAtRoute(
                "get capability topic",
                new {id = capabilityId, topicId = topicId},
                new
                {
                    id = topic.Id.ToString(),
                    name = topic.Name.ToString(),
                    description = topic.Description,
                    kafkaClusterId = topic.KafkaClusterId.ToString(),
                    partitions = topic.Partitions.ToString(),
                    retention = topic.Retention.ToString(), // TODO [jandr@2023-02-10]: convert to days (that's the units for the frontend),
                    status = topic.Status switch
                    {
                        KafkaTopicStatusType.Requested => "Requested",
                        KafkaTopicStatusType.InProgress => "In Progress",
                        KafkaTopicStatusType.Provisioned => "Provisioned",
                        _ => "Unknown"
                    },
                }
            );
        }
        catch (EntityAlreadyExistsException err)
        {
            return Results.Conflict(new ProblemDetails
            {
                Title = "Topic already exists",
                Detail = err.Message,
            });
        }
    }
}