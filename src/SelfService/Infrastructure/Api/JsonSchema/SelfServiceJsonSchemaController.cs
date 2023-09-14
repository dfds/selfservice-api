using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.SelfServiceJsonSchema;

namespace SelfService.Infrastructure.Api.JsonSchema;

[Route("json-schema")]
[Produces("application/json")]
[ApiController]
public class SelfServiceJsonSchemaController : ControllerBase
{
    private ISelfServiceJsonSchemaService _selfServiceJsonSchemaService;

    public SelfServiceJsonSchemaController(ISelfServiceJsonSchemaService selfServiceJsonSchemaService)
    {
        _selfServiceJsonSchemaService = selfServiceJsonSchemaService;
    }

    [HttpGet("{object_id:required}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetSchema(
        string objectId,
        [FromQuery] int schemaVersion = ISelfServiceJsonSchemaService.LatestVersionNumber
    )
    {
        if (SelfServiceJsonSchemaObjectId.TryParse(objectId, out var parsedObjectId))
            return BadRequest();

        try
        {
            var selfServiceJsonSchema = await _selfServiceJsonSchemaService.GetSchema(parsedObjectId, schemaVersion);
            return Ok(selfServiceJsonSchema);
        }
        catch (Exception e)
        {
            return CustomObjectResults.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"GetSchema: {e.Message}." }
            );
        }
    }

    [HttpPost("{object_id:required}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> AddSchema(
        string objectId,
        [FromBody] AddSelfServiceJsonSchemaRequest addSelfServiceJsonSchemaRequest
    )
    {
        if (SelfServiceJsonSchemaObjectId.TryParse(objectId, out var parsedObjectId))
            return BadRequest();

        if (addSelfServiceJsonSchemaRequest.Schema == null)
            return BadRequest();

        try
        {
            var selfServiceJsonSchema = await _selfServiceJsonSchemaService.AddSchema(
                parsedObjectId,
                addSelfServiceJsonSchemaRequest.Schema
            );
            return Ok(selfServiceJsonSchema);
        }
        catch (Exception e)
        {
            return CustomObjectResults.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"AddSchema: {e.Message}." }
            );
        }
    }
}
