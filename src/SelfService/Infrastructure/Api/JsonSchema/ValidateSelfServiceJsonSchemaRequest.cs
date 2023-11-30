using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace SelfService.Infrastructure.Api.JsonSchema;

public class ValidateSelfServiceJsonSchemaRequest
{
    [Required]
    public JsonObject? Schema { get; set; } = null;
}
