using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Kafka;

[Route("/kafkatopics")]
[ApiController]
public class KafkaTopicController : ControllerBase
{
    private readonly ILogger<KafkaTopicController> _logger;
    private readonly IKafkaTopicRepository _kafkaTopicRepository;
    private readonly IMessageContractRepository _messageContractRepository;
    private readonly IMembershipQuery _membershipQuery;
    private readonly ApiResourceFactory _apiResourceFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly IKafkaClusterRepository _clusterRepository;
    private readonly IKafkaTopicApplicationService _kafkaTopicApplicationService;

    public KafkaTopicController(ILogger<KafkaTopicController> logger, IKafkaTopicRepository kafkaTopicRepository, 
        IMessageContractRepository messageContractRepository, IMembershipQuery membershipQuery, 
        ApiResourceFactory apiResourceFactory, IAuthorizationService authorizationService, 
        IKafkaClusterRepository clusterRepository, IKafkaTopicApplicationService kafkaTopicApplicationService)
    {
        _logger = logger;
        _kafkaTopicRepository = kafkaTopicRepository;
        _messageContractRepository = messageContractRepository;
        _membershipQuery = membershipQuery;
        _apiResourceFactory = apiResourceFactory;
        _authorizationService = authorizationService;
        _clusterRepository = clusterRepository;
        _kafkaTopicApplicationService = kafkaTopicApplicationService;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(KafkaTopicListApiResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTopics()
    {
        var topics = await _kafkaTopicRepository.GetAllPublic();
        var clusters = await _clusterRepository.GetAll();

        return Ok(_apiResourceFactory.Convert(topics, clusters));
    }

    [HttpGet("{id:required}")]
    [ProducesResponseType(typeof(KafkaTopicApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetTopic(string id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!KafkaTopicId.TryParse(id, out var kafkaTopicId))
        {
            return NotFound();
        }

        var topic = await _kafkaTopicRepository.FindBy(kafkaTopicId);
        if (topic is null)
        {
            return NotFound();
        }

        var accessLevel = await _authorizationService.GetUserAccessLevelForCapability(userId, topic.CapabilityId);
        if (accessLevel == UserAccessLevelOptions.Read && !topic.IsPublic)
        {
            return Unauthorized($"Topic \"{topic.Name}\" belongs to a capability that user \"{userId}\" does not have access to.");
        }

        return Ok(_apiResourceFactory.Convert(topic, accessLevel));
    }

    [HttpGet("{id:required}/messagecontracts")]
    [ProducesResponseType(typeof(KafkaTopicApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetMessageContracts(string id)
    {
        if (!User.TryGetUserId(userId: out var userId))
        {
            return Unauthorized();
        }

        if (!KafkaTopicId.TryParse(text: id, id: out var kafkaTopicId))
        {
            return NotFound();
        }

        var topic = await _kafkaTopicRepository.FindBy(id: kafkaTopicId);
        if (topic is null)
        {
            return NotFound();
        }

        var hasAccess = await _membershipQuery.HasActiveMembership(userId: userId, capabilityId: topic.CapabilityId);
        if (topic.IsPrivate && !hasAccess)
        {
            return Unauthorized(value: $"Topic \"{topic.Name}\" belongs to a capability that user \"{userId}\" does not have access to.");
        }

        var contracts = await _messageContractRepository.FindBy(topicId: kafkaTopicId);

        return Ok(_apiResourceFactory.Convert(
            contracts: contracts,
            kafkaTopicId: kafkaTopicId,
            accessLevel: hasAccess 
                ? UserAccessLevelOptions.ReadWrite 
                : UserAccessLevelOptions.Read)
        );
    }

    [HttpGet("{id:required}/messagecontracts/{contractId:required}")] // NOTE [jandr@2023-03-16]: consider moving this to a root resource instead e.g. /messagecontracts/{id}
    public async Task<IActionResult> GetSingleMessageContract(string id, string contractId)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!KafkaTopicId.TryParse(id, out var kafkaTopicId))
        {
            return NotFound();
        }

        if (!MessageContractId.TryParse(contractId, out var messageContractId))
        {
            return NotFound();
        }

        var topic = await _kafkaTopicRepository.FindBy(kafkaTopicId);
        if (topic is null)
        {
            return NotFound();
        }

        var hasMembership = await _membershipQuery.HasActiveMembership(userId, topic.CapabilityId);
        if (topic.IsPrivate && !hasMembership)
        {
            return Unauthorized($"Topic \"{topic.Name}\" belongs to a capability that user \"{userId}\" does not have access to.");
        }

        var contract = await _messageContractRepository.FindBy(messageContractId);
        if (contract is null)
        {
            return NotFound();
        }
        
        return Ok(_apiResourceFactory.Convert(contract));
    }

    [HttpPost("{id:required}/messagecontracts")]
    [ProducesResponseType(typeof(KafkaTopicApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> AddMessageContract(string id, [FromBody] NewMessageContractRequest payload)
    {
        if (!User.TryGetUserId(userId: out var userId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Access denied!",
                Detail = $"User could not be granted access to adding message contracts.",
            });
        }

        if (!KafkaTopicId.TryParse(id, out var kafkaTopicId))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Kafka topic not found",
                Detail = $"Kafka topic with id \"{id}\" is not known by the system."
            });
        }

        if (!MessageType.TryParse(payload.MessageType, out var messageType))
        {
            ModelState.AddModelError(nameof(payload.MessageType), $"Value \"{payload.MessageType}\" is not a valid message type.");
        }

        if (string.IsNullOrWhiteSpace(payload.Description))
        {
            ModelState.AddModelError(nameof(payload.Description), "Value for description cannot be empty.");
        }

        if (!MessageContractExample.TryParse(payload.Example, out var contractExample))
        {
            ModelState.AddModelError(nameof(payload.Example), $"Value \"{payload.Example}\" is not a valid example.");
        }

        if (!MessageContractSchema.TryParse(payload.Schema, out var contractSchema))
        {
            ModelState.AddModelError(nameof(payload.Schema), $"Value \"{payload.Schema}\" is not a valid schema.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        var topic = await _kafkaTopicRepository.FindBy(kafkaTopicId);
        if (topic is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Kafka topic not found",
                Detail = $"Kafka topic with id \"{id}\" is not known by the system."
            });
        }

        var hasAccess = await _membershipQuery.HasActiveMembership(userId, topic.CapabilityId);
        if (topic.IsPrivate && !hasAccess)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Access denied!",
                Detail = $"Topic \"{topic.Name}\" belongs to a capability that user \"{userId}\" does not have access to."
            });
        }

        try
        {
            var messageContractId = await _kafkaTopicApplicationService.RequestNewMessageContract(
                kafkaTopicId: kafkaTopicId,
                messageType: messageType,
                description: payload.Description!,
                example: contractExample,
                schema: contractSchema,
                requestedBy: userId
            );

            var messageContract = await _messageContractRepository.Get(messageContractId);
            return Ok(_apiResourceFactory.Convert(messageContract));
        }
        catch (EntityAlreadyExistsException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Message type already exists",
                Detail = $"Topic \"{topic.Name}\" already has a message with message type \"{messageType}\"."
            });
        }
    }
}