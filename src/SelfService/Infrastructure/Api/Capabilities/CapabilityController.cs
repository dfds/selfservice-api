using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelfService.Application;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.Api.Capabilities;

[Route("capabilities")]
[Produces("application/json")]
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
    private readonly ICapabilityApplicationService _capabilityApplicationService;
    private readonly IAwsAccountRepository _awsAccountRepository;
    private readonly IAwsAccountApplicationService _awsAccountApplicationService;

    public CapabilityController(LinkGenerator linkGenerator, ICapabilityMembersQuery membersQuery, 
        ICapabilityRepository capabilityRepository, IKafkaTopicRepository kafkaTopicRepository, ApiResourceFactory apiResourceFactory, 
        IAuthorizationService authorizationService, IKafkaClusterRepository kafkaClusterRepository, 
        ICapabilityApplicationService capabilityApplicationService, IAwsAccountRepository awsAccountRepository,
        IAwsAccountApplicationService awsAccountApplicationService)
    {
        _linkGenerator = linkGenerator;
        _membersQuery = membersQuery;
        _capabilityRepository = capabilityRepository;
        _kafkaTopicRepository = kafkaTopicRepository;
        _apiResourceFactory = apiResourceFactory;
        _authorizationService = authorizationService;
        _kafkaClusterRepository = kafkaClusterRepository;
        _capabilityApplicationService = capabilityApplicationService;
        _awsAccountRepository = awsAccountRepository;
        _awsAccountApplicationService = awsAccountApplicationService;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(CapabilityListApiResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCapabilities()
    {
        var capabilities = await _capabilityRepository.GetAll();

        return Ok(new CapabilityListApiResource
        {
            Items = capabilities
                .Select(_apiResourceFactory.ConvertToListItem)
                .ToArray(),
            Links =
            {
                Self = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(HttpContext, nameof(GetAllCapabilities)) ?? "",
                    Rel = "self",
                    Allow = {"GET"}
                }
            }
        });
    }
    
    [HttpPost("")]
    public async Task<IActionResult> CreateNewCapability([FromBody] NewCapabilityRequest request)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!CapabilityId.TryCreateFrom(request.Name, out var capabilityId))
        {
            ModelState.AddModelError(nameof(request.Name),$"unable to create capability ID from name \"{request.Name}\"");
        }
        
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        try
        {
            await _capabilityApplicationService.CreateNewCapability(capabilityId, request.Name!, request.Description ?? "", userId);
        }
        catch (EntityAlreadyExistsException)
        {
            ModelState.AddModelError(nameof(request.Name),$"The name \"{request.Name}\" results in an ID that already exists");
            return ValidationProblem(statusCode: StatusCodes.Status409Conflict);
        }
       
        var capability = await _capabilityRepository.Get(capabilityId);

        return CreatedAtAction(
            actionName: nameof(GetCapabilityById),
            controllerName: "Capability",
            routeValues: new {id = capability.Id},
            value: _apiResourceFactory.Convert(capability)
        );

    }
    
    [HttpGet("{id:required}")]
    [ProducesResponseType(typeof(CapabilityDetailsApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetCapabilityById(string id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Access denied!",
                Detail = $"The user id is not valid and access to the resource cannot be granted.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        if (!CapabilityId.TryParse(id, out var capabilityId))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Capability not found",
                Detail = $"No capability with id \"{id}\" is know by the system.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var capability = await _capabilityRepository.FindBy(capabilityId);
        if (capability is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Capability not found",
                Detail = $"No capability with id \"{id}\" is know by the system.",
                Status = StatusCodes.Status404NotFound
            });
        }
        
        return Ok(await _apiResourceFactory.Convert(capability));
    }

    [HttpGet("{id:required}/members")]
    [ProducesResponseType(typeof(CapabilityMembersApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
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

        return Ok(new CapabilityMembersApiResource
        {
            Items = members
                .Select(_apiResourceFactory.Convert)
                .ToArray(),
            Links =
            {
                Self = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(HttpContext, nameof(GetCapabilityMembers), values: new {id = id}) ?? "",
                    Rel = "self",
                    Allow = {"GET"}
                }
            }
        });
    }

    [HttpGet("{id:required}/topics")]
    [ProducesResponseType(typeof(CapabilityTopicsApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
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

        return Ok(new CapabilityTopicsApiResource
        {
            Items = topics
                .Select(topic  => _apiResourceFactory.Convert(topic, accessLevel))
                .ToArray(),
            Embedded = 
            {
                KafkaClusters = _apiResourceFactory.Convert(clusters)
            },
            Links =
            {
                Self = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(HttpContext, nameof(GetCapabilityTopics), values: new {id = id}) ?? "",
                    Rel = "self",
                    Allow = accessLevel switch
                    {
                        UserAccessLevelOptions.ReadWrite => new List<string> {"GET", "POST"},
                        _ => new List<string> {"GET"}
                    }
                }
            }
        });
    }

    [HttpGet("{id:required}/awsaccount")]
    [ProducesResponseType(typeof(AwsAccountApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetCapabilityAwsAccount(string id)
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

        var account = await _awsAccountRepository.FindBy(capabilityId);
        if (account is null)
        {
            return NotFound();
        }

        return Ok(_apiResourceFactory.Convert(account, UserAccessLevelOptions.Read));
    }

    [HttpPost("{id:required}/awsaccount")]
    [ProducesResponseType(typeof(AwsAccountApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> RequestAwsAccount(string id, [FromServices] SelfServiceDbContext dbContext)
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
        if (accessLevel != UserAccessLevelOptions.ReadWrite)
        {
            return Unauthorized();
        }

        try
        {
            var awsAccountId = await _awsAccountApplicationService.RequestAwsAccount(capabilityId, userId);
            
            var account = await _awsAccountRepository.Get(awsAccountId);

            return Ok(_apiResourceFactory.Convert(account, UserAccessLevelOptions.Read));
        }
        catch (AlreadyHasAwsAccountException)
        {
            return Conflict();
        }
    }

    [HttpGet("{id:required}/membershipapplications")]
    [ProducesResponseType(typeof(MembershipApplicationListApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetCapabilityMembershipApplications(string id, [FromServices] ICapabilityMembershipApplicationQuery query)
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

        var applications = await query.FindPendingBy(capabilityId);
        
        var accessLevel = await _authorizationService.GetUserAccessLevelForCapability(userId, capabilityId);
        if (accessLevel == UserAccessLevelOptions.Read)
        {
            // only allow the current users own application(s)
            applications = applications
                .Where(x => x.Applicant == userId)
                .ToList();
        }

        var allowedInteractions = new List<string> {"GET"};
        if (accessLevel == UserAccessLevelOptions.Read && applications.Count() == 0)
        {
            allowedInteractions.Add("POST");
        }

        return Ok(new MembershipApplicationListApiResource
        {
            Items = applications
                .Select(application => _apiResourceFactory.Convert(application, accessLevel, userId))
                .ToArray(),
            Links =
            {
                Self = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(GetCapabilityMembershipApplications),
                        values: new {id = id}) ?? "",
                    Rel = "self",
                    Allow = allowedInteractions
                }
            }
        });
    }

    [HttpPost("{id:required}/membershipapplications")]
    [ProducesResponseType(typeof(MembershipApplicationApiResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> AddCapabilityMembershipApplications(string id, 
        [FromServices] IMembershipApplicationService membershipApplicationService,
        [FromServices] IMembershipApplicationRepository membershipApplicationRepository)
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

        try
        {
            var applicationId = await membershipApplicationService.SubmitMembershipApplication(capabilityId, userId);
            var membershipApplication = await membershipApplicationRepository.Get(applicationId);

            // TODO [jandr@2023-04-12]: refactor this! this is intimate knowledge of another controller
            return CreatedAtAction(
                actionName: "GetById",
                controllerName: "MembershipApplication",
                routeValues: new {id = applicationId.ToString()},
                value: _apiResourceFactory.Convert(membershipApplication, UserAccessLevelOptions.ReadWrite, userId)
            );
        }
        catch (EntityNotFoundException<Capability>)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Capability not found",
                Detail = $"Capability \"{capabilityId}\" is unknown by the system."
            });
        }
        catch (PendingMembershipApplicationAlreadyExistsException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Already has pending membership application",
                Detail = $"User \"{userId}\" already has a pending membership application for capability \"{capabilityId}\"."
            });
        }
        catch (AlreadyHasActiveMembershipException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Already member",
                Detail = $"User \"{userId}\" is already member of capability \"{capabilityId}\"."
            });
        }
    }

    [HttpPost("{id:required}/topics")] // TODO [jandr@2023-03-20]: refactor - have been "moved as is"
    [ProducesResponseType(typeof(KafkaTopicApiResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
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

        if (!KafkaTopicName.TryParse(topicRequest.Name, out var kafkaTopicName))
        {
            ModelState.AddModelError(nameof(topicRequest.Name), $"Value \"{topicRequest.Name}\" is not a valid kafka topic name.");
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