using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Kafka;

[Route("/kafkatopics")]
[ApiController]
public class KafkaTopicController : ControllerBase
{
    private readonly IKafkaTopicRepository _kafkaTopicRepository;
    private readonly IMessageContractRepository _messageContractRepository;
    private readonly IMembershipQuery _membershipQuery;
    private readonly LinkGenerator _linkGenerator;
    private readonly ApiResourceFactory _apiResourceFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly IKafkaClusterRepository _clusterRepository;

    public KafkaTopicController(IKafkaTopicRepository kafkaTopicRepository, IMessageContractRepository messageContractRepository, 
        IMembershipQuery membershipQuery, LinkGenerator linkGenerator, ApiResourceFactory apiResourceFactory, 
        IAuthorizationService authorizationService, IKafkaClusterRepository clusterRepository)
    {
        _kafkaTopicRepository = kafkaTopicRepository;
        _messageContractRepository = messageContractRepository;
        _membershipQuery = membershipQuery;
        _linkGenerator = linkGenerator;
        _apiResourceFactory = apiResourceFactory;
        _authorizationService = authorizationService;
        _clusterRepository = clusterRepository;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(KafkaTopicListApiResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTopics()
    {
        var topics = await _kafkaTopicRepository.GetAllPublic();
        var clusters = await _clusterRepository.GetAll();

        return Ok(new KafkaTopicListApiResource
        {
            Items = topics
                .Select(topic => _apiResourceFactory.Convert(topic, UserAccessLevelOptions.Read)) // NOTE [jandr@2023-03-20]: Hardcoding access level to read - change that!
                .ToArray(),
            Embedded =
            {
                KafkaClusters = _apiResourceFactory.Convert(clusters)
            },
            Links =
            {
                Self = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(HttpContext, nameof(GetAllTopics)) ?? "",
                    Rel = "self",
                    Allow = {"GET"}
                }
            }
        });
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

    [HttpGet(template: "{id:required}/messagecontracts")]
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
}