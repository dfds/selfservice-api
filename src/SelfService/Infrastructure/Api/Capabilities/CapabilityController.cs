using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Invitations;
using SelfService.Infrastructure.Api.RBAC;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.Api.Capabilities;

[Route("capabilities")]
[Produces("application/json")]
[RbacConfig(nameof(RbacObjectType.Capability) ,"id")]
[ApiController]
public class CapabilityController : ControllerBase
{
    private readonly ApiResourceFactory _apiResourceFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly IAwsAccountApplicationService _awsAccountApplicationService;
    private readonly IAzureResourceApplicationService _azureResourceApplicationService;
    private readonly IAwsAccountRepository _awsAccountRepository;
    private readonly IAzureResourceRepository _azureResourceRepository;
    private readonly ICapabilityApplicationService _capabilityApplicationService;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly IKafkaClusterAccessRepository _kafkaClusterAccessRepository;
    private readonly IKafkaClusterRepository _kafkaClusterRepository;
    private readonly IKafkaTopicRepository _kafkaTopicRepository;
    private readonly ITeamApplicationService _teamApplicationService;
    private readonly ILogger<CapabilityController> _logger;
    private readonly IMembershipApplicationService _membershipApplicationService;
    private readonly IMembershipRepository _membershipRepository;
    private readonly ICapabilityMembersQuery _membersQuery;
    private readonly ISelfServiceJsonSchemaService _selfServiceJsonSchemaService;
    private readonly IInvitationApplicationService _invitationApplicationService;
    private readonly ISelfAssessmentRepository _selfAssessmentRepository;
    private readonly ISelfAssessmentOptionRepository _selfAssessmentOptionRepository;
    private readonly IAwsEC2QueriesApplicationService _awsEC2QueriesApplicationService;

    public CapabilityController(
        ICapabilityMembersQuery membersQuery,
        ICapabilityRepository capabilityRepository,
        IKafkaTopicRepository kafkaTopicRepository,
        ApiResourceFactory apiResourceFactory,
        IAuthorizationService authorizationService,
        IKafkaClusterRepository kafkaClusterRepository,
        ICapabilityApplicationService capabilityApplicationService,
        IAwsAccountRepository awsAccountRepository,
        IAzureResourceRepository azureResourceRepository,
        IAwsAccountApplicationService awsAccountApplicationService,
        IAzureResourceApplicationService azureResourceApplicationService,
        IMembershipApplicationService membershipApplicationService,
        IMembershipRepository membershipRepository,
        IKafkaClusterAccessRepository kafkaClusterAccessRepository,
        ISelfServiceJsonSchemaService selfServiceJsonSchemaService,
        ILogger<CapabilityController> logger,
        ITeamApplicationService teamApplicationService,
        IInvitationApplicationService invitationApplicationService,
        ISelfAssessmentRepository selfAssessmentRepository,
        ISelfAssessmentOptionRepository selfAssessmentOptionRepository,
        IAwsEC2QueriesApplicationService awsEC2QueriesApplicationService
    )
    {
        _membersQuery = membersQuery;
        _capabilityRepository = capabilityRepository;
        _kafkaTopicRepository = kafkaTopicRepository;
        _apiResourceFactory = apiResourceFactory;
        _authorizationService = authorizationService;
        _kafkaClusterRepository = kafkaClusterRepository;
        _capabilityApplicationService = capabilityApplicationService;
        _awsAccountRepository = awsAccountRepository;
        _azureResourceRepository = azureResourceRepository;
        _awsAccountApplicationService = awsAccountApplicationService;
        _azureResourceApplicationService = azureResourceApplicationService;
        _membershipApplicationService = membershipApplicationService;
        _membershipRepository = membershipRepository;
        _kafkaClusterAccessRepository = kafkaClusterAccessRepository;
        _selfServiceJsonSchemaService = selfServiceJsonSchemaService;
        _logger = logger;
        _teamApplicationService = teamApplicationService;
        _invitationApplicationService = invitationApplicationService;
        _selfAssessmentRepository = selfAssessmentRepository;
        _selfAssessmentOptionRepository = selfAssessmentOptionRepository;
        _awsEC2QueriesApplicationService = awsEC2QueriesApplicationService;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(CapabilityListApiResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCapabilities()
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var capabilities = await _capabilityRepository.GetAll();
        var userMemberships = await _membershipRepository.GetAllMembershipsForUserId(userId);

        return Ok(_apiResourceFactory.Convert(capabilities, userMemberships));
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(CapabilityDetailsApiResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> CreateNewCapability([FromBody] NewCapabilityRequest request)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (!CapabilityId.TryCreateFrom(request.Name, out var capabilityId))
            ModelState.AddModelError(
                nameof(request.Name),
                $"unable to create capability ID from name \"{request.Name}\""
            );

        if (!ModelState.IsValid)
            return ValidationProblem();

        // See if request has valid json metadata
        var jsonMetadataResult = await _selfServiceJsonSchemaService.ValidateJsonMetadata(
            SelfServiceJsonSchemaObjectId.Capability,
            request.JsonMetadata
        );

        if (!jsonMetadataResult.IsValid())
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid metadata",
                    Detail = jsonMetadataResult.GetErrorString(),
                    Status = StatusCodes.Status400BadRequest,
                }
            );

        _logger.LogInformation(
            "Successfully parsed json meta data: {ParseResultCode}",
            jsonMetadataResult.ResultCode.ToString()
        );

        // Sanity check: should not be possible if result is valid
        if (jsonMetadataResult.JsonMetadata == null)
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Internal server error", Detail = "JsonMetadataResult is null" }
            );

        try
        {
            await _capabilityApplicationService.CreateNewCapability(
                capabilityId,
                request.Name!,
                request.Description ?? "",
                userId,
                jsonMetadataResult.JsonMetadata,
                jsonMetadataResult.JsonSchemaVersion
            );
        }
        catch (EntityAlreadyExistsException)
        {
            ModelState.AddModelError(
                nameof(request.Name),
                $"The name \"{request.Name}\" results in an ID that already exists"
            );
            return ValidationProblem(statusCode: StatusCodes.Status409Conflict);
        }

        var capability = await _capabilityRepository.Get(capabilityId);

        if (request.Invitees != null)
        {
            await _invitationApplicationService.CreateCapabilityInvitations(
                invitees: request.Invitees,
                inviter: userId,
                capability: capability
            );
        }
        return CreatedAtAction(
            nameof(GetCapabilityById),
            "Capability",
            new { id = capability.Id },
            await _apiResourceFactory.Convert(capability)
        );
    }

    [HttpGet("{id:required}")]
    [ProducesResponseType(typeof(CapabilityDetailsApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetCapabilityById(string id)
    {
        if (!User.TryGetUserId(out _))
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access denied!",
                    Detail = "The user id is not valid and access to the resource cannot be granted.",
                    Status = StatusCodes.Status401Unauthorized,
                }
            );

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound(
                new ProblemDetails
                {
                    Title = "Capability not found",
                    Detail = $"No capability with id \"{id}\" is know by the system.",
                    Status = StatusCodes.Status404NotFound,
                }
            );

        var capability = await _capabilityRepository.FindBy(capabilityId);
        if (capability is null)
            return NotFound(
                new ProblemDetails
                {
                    Title = "Capability not found",
                    Detail = $"No capability with id \"{id}\" is know by the system.",
                    Status = StatusCodes.Status404NotFound,
                }
            );

        return Ok(await _apiResourceFactory.Convert(capability));
    }

    [HttpGet("{id:required}/members")]
    [ProducesResponseType(typeof(CapabilityMembersApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> GetCapabilityMembers(string id)
    {
        if (!CapabilityId.TryParse(id, out var capabilityId))
            ModelState.AddModelError(nameof(id), $"Value \"{id}\" is not a valid capability id.");

        if (!ModelState.IsValid)
            return ValidationProblem();

        var members = await _membersQuery.FindBy(capabilityId);

        return Ok(_apiResourceFactory.Convert(id, members));
    }

    [HttpGet("{id:required}/awsaccount")]
    [ProducesResponseType(typeof(AwsAccountApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [RequiresPermission("aws", "read")]
    public async Task<IActionResult> GetCapabilityAwsAccount(string id)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound();

        if (!await _capabilityRepository.Exists(capabilityId))
            return NotFound();

        if (!await _authorizationService.CanViewAwsAccount(userId, capabilityId))
            return Unauthorized();

        var account = await _awsAccountRepository.FindBy(capabilityId);
        if (account is null)
            return NotFound();

        return Ok(await _apiResourceFactory.Convert(account));
    }

    [HttpPost("{id:required}/awsaccount")]
    [ProducesResponseType(typeof(AwsAccountApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    [RequiresPermission("aws", "create")]
    public async Task<IActionResult> RequestAwsAccount(string id)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound();

        if (!await _capabilityRepository.Exists(capabilityId))
            return NotFound();

        if (!await _authorizationService.CanRequestAwsAccount(userId, capabilityId))
            return Unauthorized();

        try
        {
            var awsAccountId = await _awsAccountApplicationService.RequestAwsAccount(capabilityId, userId);

            var account = await _awsAccountRepository.Get(awsAccountId);

            return Ok(await _apiResourceFactory.Convert(account));
        }
        catch (AlreadyHasAwsAccountException)
        {
            return Conflict();
        }
    }

    [HttpGet("{id:required}/awsaccount/information")]
    [ProducesResponseType(typeof(AwsAccountApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [RequiresPermission("aws", "read")]
    public async Task<IActionResult> GetCapabilityAwsAccountInformation(string id)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound();

        if (!await _capabilityRepository.Exists(capabilityId))
            return NotFound();

        if (!await _authorizationService.CanViewAwsAccount(userId, capabilityId))
            return Unauthorized();

        var account = await _awsAccountRepository.FindBy(capabilityId);
        if (account is null)
            return NotFound();

        if (account.Registration is null || account.Registration?.AccountId is null)
        {
            return NotFound();
        }

        try
        {
            var vpcs = _awsEC2QueriesApplicationService.GetVPCsAsync(account.Registration.AccountId.ToString());
            var accountInformation = new AwsAccountInformation(account.Id, capabilityId, await vpcs);
            return Ok(await _apiResourceFactory.Convert(accountInformation));
        }
        catch (Exception ex)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Error",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError,
                }
            );
        }
    }

    [HttpGet("{id:required}/azureresources")]
    [ProducesResponseType(typeof(AwsAccountApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [RequiresPermission("azure", "read")]
    public async Task<IActionResult> GetCapabilityAzureResources(string id)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound();
        if (!await _capabilityRepository.Exists(capabilityId))
            return NotFound();
        if (!await _authorizationService.CanViewAzureResources(userId, capabilityId))
            return Unauthorized();

        var resources = await _azureResourceRepository.GetFor(capabilityId);

        return Ok(await _apiResourceFactory.Convert(resources, capabilityId));
    }

    [HttpPost("{id:required}/azureresources")]
    [ProducesResponseType(typeof(AwsAccountApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    [RequiresPermission("azure", "create")]
    public async Task<IActionResult> RequestCapabilityAzureResource(
        string id,
        [FromBody] NewAzureResourceRequest request
    )
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound();

        if (!await _capabilityRepository.Exists(capabilityId))
            return NotFound();

        // todo: check legality of environment

        if (request?.environment == null)
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid metadata",
                    Detail = "Metadata missing environment empty",
                    Status = StatusCodes.Status400BadRequest,
                }
            );

        if (await _azureResourceRepository.Exists(capabilityId, request.environment))
            return Conflict();

        if (!await _authorizationService.CanRequestAzureResource(userId, capabilityId, request.environment))
            return Unauthorized();

        try
        {
            var azureResourceId = await _azureResourceApplicationService.RequestAzureResource(
                capabilityId,
                request.environment,
                userId
            );

            var resource = await _azureResourceRepository.Get(azureResourceId);

            return Ok(await _apiResourceFactory.Convert(resource));
        }
        catch (AlreadyHasAzureResourceException)
        {
            return Conflict();
        }
        catch (MissingMandatoryJsonMetadataException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id:required}/self-assessments")]
    [ProducesResponseType(typeof(AwsAccountApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [RequiresPermission("capability-management", "read-self-assess")]
    public async Task<IActionResult> GetSelfAssessments(string id)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound();

        if (!await _capabilityRepository.Exists(capabilityId))
            return NotFound();

        if (!await _authorizationService.CanSelfAssess(userId, capabilityId))
            return Unauthorized();

        var existingSelfAssessments = await _selfAssessmentRepository.GetSelfAssessmentsForCapability(capabilityId);
        var selfAssesmentOptions = await _selfAssessmentOptionRepository.GetActiveSelfAssessmentOptions();

        return Ok(await _apiResourceFactory.Convert(existingSelfAssessments, selfAssesmentOptions, capabilityId));
    }

    [HttpPost("{id:required}/self-assessments")]
    [ProducesResponseType(typeof(AwsAccountApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [RequiresPermission("capability-management", "create-self-assess")]
    public async Task<IActionResult> UpdateSelfAssessment(
        string id,
        [FromBody] SelfAssessmentRequest selfAssessmentRequest
    )
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound();

        if (!await _capabilityRepository.Exists(capabilityId))
            return NotFound();

        if (!await _authorizationService.CanSelfAssess(userId, capabilityId))
            return Unauthorized();

        if (
            !SelfAssessmentOptionId.TryParse(
                selfAssessmentRequest.SelfAssessmentOptionId,
                out var selfAssessmentOptionId
            )
        )
            return NotFound();

        if (!await _capabilityApplicationService.SelfAssessmentOptionExists(capabilityId, selfAssessmentOptionId))
            return BadRequest();

        if (!SelfAssessmentStatus.TryParse(selfAssessmentRequest.SelfAssessmentStatus, out var selfAssessmentStatus))
            return BadRequest();

        await _capabilityApplicationService.UpdateSelfAssessment(
            capabilityId,
            selfAssessmentOptionId,
            userId,
            selfAssessmentStatus
        );

        return Ok();
    }

    [HttpGet("{id:required}/azureresources/{rid:required}")]
    [ProducesResponseType(typeof(AwsAccountApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [RequiresPermission("azure", "read")]
    public async Task<IActionResult> GetCapabilityAzureResource(string id, string rid)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound();

        if (!await _capabilityRepository.Exists(capabilityId))
            return NotFound();

        if (!AzureResourceId.TryParse(rid, out var resourceId))
            return NotFound();

        if (!await _authorizationService.CanViewAzureResources(userId, capabilityId))
            return Unauthorized();

        var resource = await _azureResourceRepository.Get(resourceId);

        return Ok(await _apiResourceFactory.Convert(resource));
    }

    [HttpGet("{id:required}/membershipapplications")]
    [ProducesResponseType(typeof(MembershipApplicationListApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetCapabilityMembershipApplications(
        string id,
        [FromServices] ICapabilityMembershipApplicationQuery query
    )
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound();

        if (!await _capabilityRepository.Exists(capabilityId))
            return NotFound();

        var applications = await query.FindPendingBy(capabilityId);

        if (!await _authorizationService.CanViewAllApplications(userId, capabilityId))
            // only allow the current users own application(s)
            applications = applications.Where(x => x.Applicant == userId).ToList();

        var resource = await _apiResourceFactory.Convert(capabilityId, applications, userId);

        return Ok(resource);
    }

    [HttpPost("{id:required}/membershipapplications")]
    [ProducesResponseType(typeof(MembershipApplicationApiResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> AddCapabilityMembershipApplications(
        string id,
        [FromServices] IMembershipApplicationService membershipApplicationService,
        [FromServices] IMembershipApplicationRepository membershipApplicationRepository
    )
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound();

        if (!await _capabilityRepository.Exists(capabilityId))
            return NotFound();

        try
        {
            var applicationId = await membershipApplicationService.SubmitMembershipApplication(capabilityId, userId);
            var membershipApplication = await membershipApplicationRepository.Get(applicationId);

            // TODO [jandr@2023-04-12]: refactor this! this is intimate knowledge of another controller
            return CreatedAtAction(
                "GetById",
                "MembershipApplication",
                new { id = applicationId.ToString() },
                _apiResourceFactory.Convert(membershipApplication, userId)
            );
        }
        catch (EntityNotFoundException<Capability>)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Capability not found",
                    Detail = $"Capability \"{capabilityId}\" is unknown by the system.",
                }
            );
        }
        catch (PendingMembershipApplicationAlreadyExistsException)
        {
            return Conflict(
                new ProblemDetails
                {
                    Title = "Already has pending membership application",
                    Detail =
                        $"User \"{userId}\" already has a pending membership application for capability \"{capabilityId}\".",
                }
            );
        }
        catch (AlreadyHasActiveMembershipException)
        {
            return Conflict(
                new ProblemDetails
                {
                    Title = "Already member",
                    Detail = $"User \"{userId}\" is already member of capability \"{capabilityId}\".",
                }
            );
        }
    }

    [HttpPost("{id:required}/topics")] // TODO [jandr@2023-03-20]: refactor - have been "moved as is"
    [ProducesResponseType(typeof(KafkaTopicApiResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    [RequiresPermission("topics", "create")]
    public async Task<IActionResult> AddCapabilityTopic(string id, [FromBody] NewKafkaTopicRequest topicRequest)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound();

        if (!await _capabilityRepository.Exists(capabilityId))
            return NotFound();

        if (!KafkaClusterId.TryParse(topicRequest.KafkaClusterId, out var kafkaClusterId))
            ModelState.AddModelError(
                nameof(topicRequest.KafkaClusterId),
                $"Value \"{topicRequest.KafkaClusterId}\" is not a valid kafka cluster id."
            );

        if (!KafkaTopicName.TryParse(topicRequest.Name, out var kafkaTopicName))
            ModelState.AddModelError(
                nameof(topicRequest.Name),
                $"Value \"{topicRequest.Name}\" is not a valid kafka topic name."
            );

        if (!KafkaTopicPartitions.TryCreate(topicRequest.Partitions ?? 0, out var topicPartitions))
            ModelState.AddModelError(
                nameof(topicRequest.Partitions),
                $"Value \"{topicRequest.Partitions}\" is invalid for kafka topic partitions."
            );

        if (!KafkaTopicRetention.TryParse(topicRequest.Retention, out var topicRetention))
            ModelState.AddModelError(
                nameof(topicRequest.Retention),
                $"Value \"{topicRequest.Retention}\" is invalid for kafka topic retention."
            );

        if (!await _kafkaClusterRepository.Exists(kafkaClusterId))
            ModelState.AddModelError(
                nameof(topicRequest.KafkaClusterId),
                $"Kafka cluster with id \"{kafkaClusterId}\" is unknown to the system."
            );

        if (!ModelState.IsValid)
            return ValidationProblem();

        if (!await _authorizationService.CanAdd(userId, capabilityId, kafkaClusterId))
            return Unauthorized();

        try
        {
            var topicId = await _capabilityApplicationService.RequestNewTopic(
                capabilityId,
                kafkaClusterId,
                kafkaTopicName,
                topicRequest.Description ?? "",
                topicPartitions,
                topicRetention,
                userId
            );

            var topic = await _kafkaTopicRepository.Get(topicId);

            return CreatedAtAction(
                "GetTopic",
                "KafkaTopic",
                new { id = topic.Id },
                await _apiResourceFactory.Convert(topic)
            );
        }
        catch (EntityAlreadyExistsException err)
        {
            return Conflict(new ProblemDetails { Title = "Topic already exists", Detail = err.Message });
        }
    }

    [HttpPost("{id}/leave")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> LeaveCapability(string id)
    {
        // Verify user and fetch userId
        if (!User.TryGetUserId(out var userId))
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unknown user id",
                    Detail = "User id is not valid and thus cannot leave any capabilities.",
                }
            );

        // Check that capability with provided id exists
        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound(
                new ProblemDetails
                {
                    Title = "Capability not found.",
                    Detail = $"A capability with id \"{id}\" could not be found.",
                }
            );

        // Leave capability
        try
        {
            await _membershipApplicationService.LeaveCapability(capabilityId, userId);
            return NoContent();
        }
        catch (EntityNotFoundException<Membership>)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Membership not cancelled.",
                    Detail = $"A membership of user \"{userId}\" for capability \"{id}\" could not be found.",
                }
            );
        }
    }

    [HttpGet("{id:required}/kafkaclusteraccess")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> GetKafkaClusterAccessList(string id)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unknown user id",
                    Detail = "User id is not valid and thus cannot leave any capabilities.",
                }
            );

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound(
                new ProblemDetails
                {
                    Title = "Capability not found.",
                    Detail = $"A capability with id \"{id}\" could not be found.",
                }
            );

        var capability = await _capabilityRepository.FindBy(capabilityId);
        if (capability == null)
            return NotFound(
                new ProblemDetails
                {
                    Title = "Capability not found.",
                    Detail = $"A capability with id \"{id}\" could not be found.",
                }
            );

        var clusters = await _kafkaClusterRepository.GetAll();

        return Ok(await _apiResourceFactory.Convert(capabilityId, clusters));
    }

    [HttpGet("{id:required}/kafkaclusteraccess/{clusterId:required}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetKafkaClusterAccess(string id, string clusterId)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unknown user id",
                    Detail = "User id is not valid and thus cannot leave any capabilities.",
                }
            );

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound(
                new ProblemDetails
                {
                    Title = "Capability not found.",
                    Detail = $"A capability with id \"{id}\" could not be found.",
                }
            );

        if (!KafkaClusterId.TryParse(clusterId, out var kafkaClusterId))
            return NotFound(
                new ProblemDetails
                {
                    Title = "Kafka cluster not found.",
                    Detail = $"A Kafka cluster with id \"{clusterId}\" could not be found.",
                }
            );

        if (!await _authorizationService.CanViewAccess(userId, capabilityId))
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Not a capability member",
                    Detail = $"User is not a member of capability {capabilityId}",
                }
            );

        var kafkaCluster = await _kafkaClusterRepository.FindBy(kafkaClusterId);
        if (kafkaCluster == null)
            return NotFound(
                new ProblemDetails
                {
                    Title = "Kafka cluster not found.",
                    Detail = $"Kafka cluster \"{clusterId}\" could has not found.",
                }
            );

        var clusterAccess = await _kafkaClusterAccessRepository.FindBy(capabilityId, kafkaClusterId);
        if (clusterAccess == null)
            return NotFound(
                new ProblemDetails
                {
                    Title = "Kafka cluster access not found.",
                    Detail =
                        $"Access to Kafka cluster \"{clusterId}\" for capability \"{capabilityId}\" has not been requested.",
                }
            );

        if (clusterAccess.IsAccessGranted)
            return Ok(
                new KafkaClusterAccessApiResource(kafkaCluster.BootstrapServers, kafkaCluster.SchemaRegistryUrl, null)
            );

        return AcceptedAtAction(
            nameof(GetCapabilityById),
            "Capability",
            new { id, clusterId },
            new { status = "Requested" }
        );
    }

    [HttpPost("{id:required}/kafkaclusteraccess/{clusterId:required}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [RequiresPermission("topics", "create")]
    public async Task<IActionResult> RequestKafkaClusterAccess(string id, string clusterId)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unknown user id",
                    Detail = "User id is not valid and thus cannot leave any capabilities.",
                }
            );

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound(
                new ProblemDetails
                {
                    Title = "Capability not found.",
                    Detail = $"A capability with id \"{id}\" could not be found.",
                }
            );

        if (!KafkaClusterId.TryParse(clusterId, out var kafkaClusterId))
            return NotFound(
                new ProblemDetails
                {
                    Title = "Kafka cluster not found.",
                    Detail = $"A Kafka cluster with id \"{clusterId}\" could not be found.",
                }
            );

        await _capabilityApplicationService.RequestKafkaClusterAccess(capabilityId, kafkaClusterId, userId);

        return AcceptedAtAction(
            nameof(GetCapabilityById),
            "Capability",
            new { id, clusterId },
            new { status = "Requested" }
        );
    }

    [HttpPost("{id}/requestdeletion")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [RequiresPermission("capability-management", "request-deletion")]
    public async Task<IActionResult> RequestCapabilityDeletion(string id, UserId user)
    {
        // Verify user and fetch userId
        if (!User.TryGetUserId(out var userId))
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unknown user id",
                    Detail = "User id is not valid and thus cannot leave any capabilities.",
                }
            );

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound(
                new ProblemDetails
                {
                    Title = "Capability not found.",
                    Detail = $"A capability with id \"{id}\" could not be found.",
                }
            );

        // deleting and canceling deletion are the same responsibility currently.
        // thus we use the same authorization check for both
        if (!await _authorizationService.CanDeleteCapability(userId, capabilityId))
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Not authorized",
                    Detail =
                        $"User \"{userId}\" is not authorized to cancel deletion request for capability \"{capabilityId}\".",
                }
            );

        // Set capability status
        try
        {
            await _capabilityApplicationService.RequestCapabilityDeletion(capabilityId, userId);
            return NoContent();
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(
                new ProblemDetails { Title = "Capability deletion not requested.", Detail = $"error: {e.Message}" }
            );
        }
        catch (InvalidOperationException)
        {
            // fails silently
            // users triggering this are trying to circumvent the system and we ignore them
            return NoContent();
        }
    }

    [HttpPost("{id}/canceldeletionrequest")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [RequiresPermission("capability-management", "request-deletion")]
    public async Task<IActionResult> CancelCapabilityDeletionRequest(string id)
    {
        // Verify user and fetch userId
        if (!User.TryGetUserId(out var userId))
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unknown user id",
                    Detail = "User id is not valid and thus cannot leave any capabilities.",
                }
            );

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound(
                new ProblemDetails
                {
                    Title = "Capability not found.",
                    Detail = $"A capability with id \"{id}\" could not be found.",
                }
            );

        // deleting and canceling deletion are the same responsibility currently.
        // thus we use the same authorization check for both -- if you can do one, you can do the other
        if (!await _authorizationService.CanDeleteCapability(userId, capabilityId))
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Not authorized",
                    Detail =
                        $"User \"{userId}\" is not authorized to cancel deletion request for capability \"{capabilityId}\".",
                }
            );

        // Set capability status
        try
        {
            await _capabilityApplicationService.CancelCapabilityDeletionRequest(capabilityId, userId);
            return NoContent();
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(
                new ProblemDetails { Title = "Capability deletion not requested.", Detail = $"error: {e.Message}" }
            );
        }
        catch (InvalidOperationException)
        {
            // fails silently
            // users triggering this are trying to circumvent the system and we ignore them
            return NoContent();
        }
    }

    [HttpGet("{id}/metadata")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetCapabilityMetadata(string id)
    {
        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound(
                new ProblemDetails
                {
                    Title = "Capability not found.",
                    Detail = $"A capability with id \"{id}\" could not be found.",
                }
            );

        var metadata = await _capabilityApplicationService.GetJsonMetadata(capabilityId);
        return Ok(metadata);
    }

    [HttpPost("{id}/required-metadata")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [RequiresPermission("tags-and-metadata", "create")]
    [RequiresPermission("tags-and-metadata", "update")]
    public async Task<IActionResult> SetCapabilityRequiredMetadata(
        string id,
        [FromBody] SetCapabilityMetadataRequest request
    )
    {
        // Verify user and fetch userId
        if (!User.TryGetUserId(out var userId))
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unknown user id",
                    Detail = "User id is not valid and thus set capability metadata.",
                }
            );

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound(
                new ProblemDetails
                {
                    Title = "Capability not found.",
                    Detail = $"A capability with id \"{id}\" could not be found.",
                }
            );

        var portalUser = HttpContext.User.ToPortalUser();
        if (!await _authorizationService.CanGetSetCapabilityJsonMetadata(portalUser, capabilityId))
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Not authorized",
                    Detail = $"User \"{userId}\" is not authorized to set capability metadata.",
                }
            );

        if (request?.JsonMetadata == null)
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid metadata",
                    Detail = "Request body is empty",
                    Status = StatusCodes.Status400BadRequest,
                }
            );

        var jsonString = request.JsonMetadata.ToJsonString();

        try
        {
            await _capabilityApplicationService.SetJsonMetadata(capabilityId, jsonString);
        }
        catch (InvalidJsonMetadataException e)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid json metadata",
                    Detail = e.Message,
                    Status = StatusCodes.Status400BadRequest,
                }
            );
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"SetCapabilityMetadata: {e.Message}." }
            );
        }

        return Ok();
    }

    [HttpPost("{id}/metadata")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [RequiresPermission("tags-and-metadata", "create")]
    [RequiresPermission("tags-and-metadata", "update")]
    public async Task<IActionResult> SetCapabilityMetadata(string id, [FromBody] SetCapabilityMetadataRequest request)
    {
        // Verify user and fetch userId
        if (!User.TryGetUserId(out var userId))
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unknown user id",
                    Detail = "User id is not valid and thus set capability metadata.",
                }
            );

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound(
                new ProblemDetails
                {
                    Title = "Capability not found.",
                    Detail = $"A capability with id \"{id}\" could not be found.",
                }
            );

        var portalUser = HttpContext.User.ToPortalUser();
        if (!await _authorizationService.CanGetSetCapabilityJsonMetadata(portalUser, capabilityId))
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Not authorized",
                    Detail = $"User \"{userId}\" is not authorized to set capability metadata.",
                }
            );

        if (request?.JsonMetadata == null)
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid metadata",
                    Detail = "Request body is empty",
                    Status = StatusCodes.Status400BadRequest,
                }
            );

        try
        {
            await _capabilityApplicationService.SetJsonMetadata(capabilityId, request.JsonMetadata.ToJsonString());
        }
        catch (InvalidJsonMetadataException e)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid json metadata",
                    Detail = e.Message,
                    Status = StatusCodes.Status400BadRequest,
                }
            );
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"SetCapabilityMetadata: {e.Message}." }
            );
        }

        return Ok();
    }

    [HttpGet("{id}/teams")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> GetLinkedTeams(string id)
    {
        if (!CapabilityId.TryParse(id, out var capabilityId))
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Capability not found.",
                    Detail = $"A capability with id \"{id}\" could not be found.",
                }
            );
        }

        var teams = await _teamApplicationService.GetLinkedTeams(capabilityId);
        return Ok(teams);
    }

    [HttpPost("{id}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Join(string id)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unknown user id",
                    Detail = "User id is not valid and thus set capability metadata.",
                }
            );

        if (!CapabilityId.TryParse(id, out var capabilityId))
            return NotFound(
                new ProblemDetails
                {
                    Title = "Capability not found.",
                    Detail = $"A capability with id \"{id}\" could not be found.",
                }
            );
        var portalUser = HttpContext.User.ToPortalUser();
        if (!_authorizationService.CanBypassMembershipApprovals(portalUser))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "User unauthorized",
                    Detail = $"user \"{userId}\" isn't authorized to join capabilities directly.",
                }
            );
        }
        try
        {
            await _membershipApplicationService.JoinCapability(id, portalUser.Id);
        }
        catch (AlreadyHasActiveMembershipException e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails
                {
                    Title = "User already in capability",
                    Detail = $"AddUserToCapability: {e.Message}.",
                }
            );
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"AddUserToCapability: {e.Message}." }
            );
        }
        return Ok();
    }

    [HttpPost("{id}/invitations")]
    [ProducesResponseType(typeof(CapabilityDetailsApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [RequiresPermission("capability-membership-management", "create")]
    public async Task<IActionResult> CreateInvitations([FromRoute] string id, [FromBody] InvitationsRequest request)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!CapabilityId.TryParse(id, out var capabilityId))
        {
            return BadRequest(
                new ProblemDetails { Title = "Invalid CapabilityId provided", Status = StatusCodes.Status400BadRequest }
            );
        }

        if (!await _authorizationService.CanInviteToCapability(userId, capabilityId))
        {
            return Unauthorized();
        }

        var capability = await _capabilityRepository.Get(capabilityId);
        if (capability is null)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Capability not found.",
                    Detail = $"A capability with id \"{id}\" could not be found.",
                }
            );
        }
        if (request.Invitees == null)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid request",
                    Detail = "Request body Invitees is not provided",
                    Status = StatusCodes.Status400BadRequest,
                }
            );
        }
        return Ok(
            await _invitationApplicationService.CreateCapabilityInvitations(
                invitees: request.Invitees,
                inviter: userId,
                capability: capability
            )
        );
    }

    [HttpGet("{id}/configurationlevel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> GetConfigurationLevel([FromRoute] string id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!CapabilityId.TryParse(id, out var capabilityId))
        {
            return BadRequest(
                new ProblemDetails { Title = "Invalid CapabilityId provided", Status = StatusCodes.Status400BadRequest }
            );
        }
        var configurationLevelInfo = await _capabilityApplicationService.GetConfigurationLevel(capabilityId);
        return Ok(configurationLevelInfo);
    }

    [HttpGet("self-assessment-options")]
    [ProducesResponseType(typeof(AwsAccountApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [RequiresPermission("capability-management", "read-self-assess")]
    public async Task<IActionResult> GetSelfAssessmentOptions()
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var portalUser = HttpContext.User.ToPortalUser();

        if (!_authorizationService.CanManageSelfAssessmentOptions(portalUser))
            return Unauthorized();

        var selfAssessmentOptions = await _selfAssessmentOptionRepository.GetAllSelfAssessmentOptions();

        return Ok(_apiResourceFactory.Convert(selfAssessmentOptions));
    }

    [HttpPost("self-assessment-options")]
    [ProducesResponseType(typeof(AwsAccountApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> AddSelfAssessmentOption([FromBody] AddSelfAssessmentOptionRequest request)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var portalUser = HttpContext.User.ToPortalUser();

        if (!_authorizationService.CanManageSelfAssessmentOptions(portalUser))
            return Unauthorized();

        var newSelfAssessmentOption = SelfAssessmentOption.New(
            userId: userId,
            shortName: request.ShortName,
            description: request.Description,
            documentationUrl: request.DocumentationUrl
        );

        await _selfAssessmentOptionRepository.AddSelfAssessmentOption(newSelfAssessmentOption);

        return Ok();
    }

    [HttpPost("self-assessment-options/{id}/update")]
    [ProducesResponseType(typeof(AwsAccountApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [RequiresPermission("capability-management", "create-self-assess")]
    public async Task<IActionResult> UpdateSelfAssessmentOption(
        [FromRoute] string id,
        [FromBody] UpdateSelfAssessmentOptionRequest request
    )
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var portalUser = HttpContext.User.ToPortalUser();

        if (!_authorizationService.CanManageSelfAssessmentOptions(portalUser))
            return Unauthorized();

        if (!SelfAssessmentOptionId.TryParse(id, out var selfAssessmentOptionId))
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid SelfAssessmentOptionId provided",
                    Status = StatusCodes.Status400BadRequest,
                }
            );
        }

        await _selfAssessmentOptionRepository.UpdateSelfAssessmentOption(
            selfAssessmentOptionId,
            request.ShortName,
            request.Description,
            request.DocumentationUrl
        );

        return Ok();
    }

    [HttpPost("self-assessment-options/{id}/activate")]
    [ProducesResponseType(typeof(AwsAccountApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> ActivateSelfAssessmentOption([FromRoute] string id)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var portalUser = HttpContext.User.ToPortalUser();

        if (!_authorizationService.CanManageSelfAssessmentOptions(portalUser))
            return Unauthorized();

        if (!SelfAssessmentOptionId.TryParse(id, out var selfAssessmentOptionId))
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid SelfAssessmentOptionId provided",
                    Status = StatusCodes.Status400BadRequest,
                }
            );
        }

        await _selfAssessmentOptionRepository.ActivateSelfAssessmentOption(selfAssessmentOptionId);

        return Ok();
    }

    [HttpPost("self-assessment-options/{id}/deactivate")]
    [ProducesResponseType(typeof(AwsAccountApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> DeactivateSelfAssessmentOption([FromRoute] string id)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var portalUser = HttpContext.User.ToPortalUser();

        if (!_authorizationService.CanManageSelfAssessmentOptions(portalUser))
            return Unauthorized();

        if (!SelfAssessmentOptionId.TryParse(id, out var selfAssessmentOptionId))
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid SelfAssessmentOptionId provided",
                    Status = StatusCodes.Status400BadRequest,
                }
            );
        }

        await _selfAssessmentOptionRepository.DeactivateSelfAssessmentOption(selfAssessmentOptionId);

        return Ok();
    }
}
