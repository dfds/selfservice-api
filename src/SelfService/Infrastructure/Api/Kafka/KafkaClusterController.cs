using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.Api.Kafka;

[Route("/kafkaclusters")]
[ApiController]
public class KafkaClusterController : ControllerBase
{
    private readonly IKafkaClusterRepository _clusterRepository;
    private readonly ApiResourceFactory _apiResourceFactory;

    public KafkaClusterController(IKafkaClusterRepository clusterRepository, ApiResourceFactory apiResourceFactory)
    {
        _clusterRepository = clusterRepository;
        _apiResourceFactory = apiResourceFactory;
    }

    [HttpGet("")]
    public async Task<IResult> GetAllClusters([FromServices] SelfServiceDbContext context)
    {
        var clusters = await _clusterRepository.GetAll();

        return Results.Ok(new ResourceListDto<KafkaClusterDto>
        {
            Items = clusters
                .Select(_apiResourceFactory.Convert)
                .ToArray()
        });
    }

    [HttpGet("{id:required}")]
    public async Task<IActionResult> GetById(string id, [FromServices] SelfServiceDbContext context)
    {
        if (!KafkaClusterId.TryParse(id, out var kafkaClusterId))
        {
            return NotFound();
        }

        var cluster = await _clusterRepository.FindBy(kafkaClusterId);
        if (cluster is null)
        {
            return NotFound();
        }

        return Ok(_apiResourceFactory.Convert(cluster));
    }
}