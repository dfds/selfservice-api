using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Infrastructure.Api.Kafka;

[Route("/kafkaschemas")]
[ApiController]
public class KafkaSchemaController : ControllerBase
{
    private readonly ILogger<KafkaSchemaController> _logger;
    private readonly IKafkaSchemaService _kafkaSchemaService;

    public KafkaSchemaController(
        ILogger<KafkaSchemaController> logger,
        IKafkaSchemaService kafkaSchemaService
    )
    {
        _logger = logger;
        _kafkaSchemaService = kafkaSchemaService;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(KafkaSchema[]), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> ListSchemas([FromQuery] KafkaSchemaQueryParams queryParams)
    {
        try
        {
            var schemas = await _kafkaSchemaService.ListSchemas(queryParams);
            return Ok(schemas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list schemas");
            return Problem("Failed to list schemas", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}