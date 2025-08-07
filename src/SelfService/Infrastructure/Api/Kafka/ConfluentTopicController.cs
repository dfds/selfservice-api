using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Kafka;

[Route("/confluent-topics")]
[ApiController]
public class ConfluentTopicController : ControllerBase
{
    private readonly ILogger<ConfluentTopicController> _logger;
    private readonly IConfluentGatewayService _confluentGatewayService;
    private readonly IKafkaClusterRepository _kafkaClusterRepository;
    private readonly ApiResourceFactory _apiResourceFactory;

    public ConfluentTopicController(ILogger<ConfluentTopicController> logger, IConfluentGatewayService confluentGatewayService, IKafkaClusterRepository kafkaClusterRepository, ApiResourceFactory apiResourceFactory)
        : base()
    {
        _logger = logger;
        _confluentGatewayService = confluentGatewayService;
        _kafkaClusterRepository = kafkaClusterRepository;
        _apiResourceFactory = apiResourceFactory;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(KafkaSchema[]), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]

    public async Task<IActionResult> GetTopicsForCluster()
    {
        _logger.LogInformation("FLUTTERSHY :: GetTopicsForCluster called");

        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails { Title = "Access denied", Detail = $"User is unknown to the system." }
            );
        }

        var clusters = await _kafkaClusterRepository.GetAll();

        _logger.LogInformation("FLUTTERSHY :: Found {Count} Kafka clusters", clusters.Count());

        if (clusters.Count() == 0)
        {
            return NotFound(
                new ProblemDetails { Title = "No clusters found", Detail = "There are no Kafka clusters available." }
            );
        }

        foreach (var cluster in clusters)
        {
            if (!KafkaClusterId.TryParse(cluster.Id, out var kafkaClusterId))
            {
                return NotFound(
                    new ProblemDetails { Title = "Cluster not found", Detail = $"Cluster with id \"{cluster.Id}\" could not be found." }
                );
            }
            var topics = await _confluentGatewayService.ListTopics(kafkaClusterId);
        }
    
        return Ok(_apiResourceFactory.Convert(clusters));
    }
}
