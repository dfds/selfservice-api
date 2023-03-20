﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelfService.Application;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.Api.Capabilities;

[Route("capabilities")]
[ApiController]
public class CapabilityController : ControllerBase
{
    private readonly LinkGenerator _linkGenerator;
    private readonly ICapabilityMembersQuery _membersQuery;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly IKafkaTopicRepository _kafkaTopicRepository;
    private readonly ApiResourceFactory _apiResourceFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly IKafkaClusterRepository _kafkaClusterRepository;
    private readonly IMembershipQuery _membershipQuery;
    private readonly ICapabilityApplicationService _capabilityApplicationService;

    public CapabilityController(LinkGenerator linkGenerator, ICapabilityMembersQuery membersQuery, 
        ICapabilityRepository capabilityRepository, IKafkaTopicRepository kafkaTopicRepository, ApiResourceFactory apiResourceFactory, 
        IAuthorizationService authorizationService, IKafkaClusterRepository kafkaClusterRepository, IMembershipQuery membershipQuery, 
        ICapabilityApplicationService capabilityApplicationService)
    {
        _linkGenerator = linkGenerator;
        _membersQuery = membersQuery;
        _capabilityRepository = capabilityRepository;
        _kafkaTopicRepository = kafkaTopicRepository;
        _apiResourceFactory = apiResourceFactory;
        _authorizationService = authorizationService;
        _kafkaClusterRepository = kafkaClusterRepository;
        _membershipQuery = membershipQuery;
        _capabilityApplicationService = capabilityApplicationService;
    }

    [HttpGet("")]
    public async Task<IActionResult> GetAllCapabilities()
    {
        var myCapabilities = new HashSet<CapabilityId>();

        if (User.TryGetUserId(out var userId))
        {
            var activeMemberships = await _membershipQuery.FindActiveBy(userId);
            myCapabilities = activeMemberships
                .Select(x => x.CapabilityId)
                .ToHashSet();
        }
        
        var capabilities = await _capabilityRepository.GetAll();

        return Ok(new CapabilityListDto()
        {
            Items = capabilities
                .Select(capability => _apiResourceFactory.Convert(
                    capability: capability,
                    accessLevel: myCapabilities.Contains(capability.Id) // TODO [jandr@2023-03-17]: move this knowledge (read/readwrite) to authorization service
                        ? UserAccessLevelOptions.ReadWrite
                        : UserAccessLevelOptions.Read
                ))
                .ToArray(),
            Links =
            {
                {
                    "self", new ResourceLink
                    {
                        Href = _linkGenerator.GetUriByAction(HttpContext, nameof(GetAllCapabilities)) ?? "",
                        Rel = "self",
                        Allow = {"GET"}
                    }
                }
            }
        });
    }

    [HttpGet("{id:required}")]
    public async Task<IActionResult> GetCapabilityById(string id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!CapabilityId.TryParse(id, out var capabilityId))
        {
            return NotFound();
        }

        var capability = await _capabilityRepository.FindBy(capabilityId);
        if (capability is null)
        {
            return NotFound();
        }

        var accessLevel = await _authorizationService.GetUserAccessLevelForCapability(userId, capabilityId);

        return Ok(_apiResourceFactory.Convert(capability, accessLevel));
    }

    [HttpGet("{id:required}/members")]
    public async Task<IActionResult> GetCapabilityMembers(string id)
    {
        if (!CapabilityId.TryParse(id, out var capabilityId))
        {
            ModelState.AddModelError(nameof(id), $"Value \"{id}\" is not a valid capability id.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        var members = await _membersQuery.FindBy(capabilityId);

        return Ok(new CapabilityMemberListDto
        {
            Items = members
                .Select(_apiResourceFactory.Convert)
                .ToArray(),
        }); 
    }

    [HttpGet("{id:required}/topics")]
    public async Task<IActionResult> GetCapabilityTopics(string id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!CapabilityId.TryParse(id, out var capabilityId))
        {
            return NotFound();
        }

        if (!await _capabilityRepository.Exists(capabilityId))
        {
            return NotFound();
        }

        var topics = await _kafkaTopicRepository.FindBy(capabilityId);

        var accessLevel = await _authorizationService.GetUserAccessLevelForCapability(userId, capabilityId);
        if (accessLevel == UserAccessLevelOptions.Read)
        {
            // only public topics are allowed if the user only has read access
            topics = topics.Where(x => x.IsPublic);
        }

        var clusters = await _kafkaClusterRepository.GetAll();

        return Ok(new CapabilityTopicsDto
        {
            Items = topics
                .Select(topic  => _apiResourceFactory.Convert(topic, accessLevel))
                .ToArray(),
            Embedded = 
            {
                {
                    "kafkaClusters", new KafkaClusterListDto
                    {
                        Items = clusters
                            .Select(_apiResourceFactory.Convert)
                            .ToArray(),
                    }
                }
            },
            Links = 
            {
                {
                    "self", new ResourceLink
                    {
                        Href = _linkGenerator.GetUriByAction(HttpContext, nameof(GetCapabilityTopics), values: new {id = id}) ?? "",
                        Rel = "self",
                        Allow = accessLevel switch
                        {
                            UserAccessLevelOptions.ReadWrite => new List<string> {"GET", "POST"},
                            _ => new List<string> {"GET"},
                        }
                    }
                }
            }
        });
    }

    [HttpGet("{id:required}/awsaccount")] // TODO [jandr@2023-03-20]: refactor - have been "moved as is"
    public async Task<IActionResult> GetCapabilityAwsAccount(string id, [FromServices] SelfServiceDbContext dbContext)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!CapabilityId.TryParse(id, out var capabilityId))
        {
            return NotFound();
        }

        if (!await _capabilityRepository.Exists(capabilityId))
        {
            return NotFound();
        }

        var accessLevel = await _authorizationService.GetUserAccessLevelForCapability(userId, capabilityId);
        if (accessLevel == UserAccessLevelOptions.Read)
        {
            return Unauthorized();
        }

        var account = await dbContext.AwsAccounts.SingleOrDefaultAsync(x => x.CapabilityId == capabilityId);
        if (account is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            Id = account.Id.ToString(),
            AwsAccountId = account.AccountId.ToString(),
            RoleArn = account.RoleArn.ToString(),
            RoleEmail = account.RoleEmail,
            CreatedAt = account.CreatedAt.ToString("O"),
        });
    }

    [HttpGet("{id:required}/membershipapplications")] // TODO [jandr@2023-03-20]: refactor - have been "moved as is"
    public async Task<IActionResult> GetCapabilityMembershipApplications(string id, [FromServices] SelfServiceDbContext dbContext)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!CapabilityId.TryParse(id, out var capabilityId))
        {
            return NotFound();
        }

        if (!await _capabilityRepository.Exists(capabilityId))
        {
            return NotFound();
        }

        var accessLevel = await _authorizationService.GetUserAccessLevelForCapability(userId, capabilityId);
        if (accessLevel == UserAccessLevelOptions.Read)
        {
            return Unauthorized();
        }

        var applications = await dbContext.MembershipApplications
            .Where(x => x.CapabilityId == capabilityId)
            .OrderBy(x => x.SubmittedAt)
            .ToListAsync();

        return Ok(new
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

    [HttpPost("{id:required}/topics")] // TODO [jandr@2023-03-20]: refactor - have been "moved as is"
    public async Task<IActionResult> AddCapabilityTopic(string id, [FromBody] NewKafkaTopicRequest topicRequest)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!CapabilityId.TryParse(id, out var capabilityId))
        {
            return NotFound();
        }

        if (!await _capabilityRepository.Exists(capabilityId))
        {
            return NotFound();
        }

        var accessLevel = await _authorizationService.GetUserAccessLevelForCapability(userId, capabilityId);
        if (accessLevel == UserAccessLevelOptions.Read)
        {
            return Unauthorized();
        }

        if (!KafkaClusterId.TryParse(topicRequest.KafkaClusterId, out var kafkaClusterId))
        {
            ModelState.AddModelError(nameof(topicRequest.KafkaClusterId), $"Value \"{topicRequest.KafkaClusterId}\" is not a valid kafka cluster id.");
        }

        if (!KafkaTopicName.TryParse(topicRequest.KafkaTopicName, out var kafkaTopicName))
        {
            ModelState.AddModelError(nameof(topicRequest.KafkaTopicName), $"Value \"{topicRequest.KafkaTopicName}\" is not a valid kafka topic name.");
        }

        if (!KafkaTopicPartitions.TryCreate(topicRequest.Partitions ?? 0, out var topicPartitions))
        {
            ModelState.AddModelError(nameof(topicRequest.Partitions), $"Value \"{topicRequest.Partitions}\" is invalid for kafka topic partitions.");
        }

        if (!KafkaTopicRetention.TryParse(topicRequest.Retention, out var topicRetention))
        {
            ModelState.AddModelError(nameof(topicRequest.Retention), $"Value \"{topicRequest.Retention}\" is invalid for kafka topic retention.");
        }

        if (!await _kafkaClusterRepository.Exists(kafkaClusterId))
        {
            ModelState.AddModelError(nameof(topicRequest.KafkaClusterId), $"Kafka cluster with id \"{kafkaClusterId}\" is unknown to the system.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        try
        {
            var topicId = await _capabilityApplicationService.RequestNewTopic(
                capabilityId: capabilityId,
                kafkaClusterId: kafkaClusterId,
                name: kafkaTopicName,
                description: topicRequest.Description ?? "",
                partitions: topicPartitions,
                retention: topicRetention,
                requestedBy: userId
            );

            var topic = await _kafkaTopicRepository.Get(topicId);

            return CreatedAtAction(
                actionName: "GetTopic",
                controllerName: "KafkaTopic",
                routeValues: new {id = topic.Id},
                value: _apiResourceFactory.Convert(topic, UserAccessLevelOptions.ReadWrite)
            );
        }
        catch (EntityAlreadyExistsException err)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Topic already exists",
                Detail = err.Message,
            });
        }
    }
}