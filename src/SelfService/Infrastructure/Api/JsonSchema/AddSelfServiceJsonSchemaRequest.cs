using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace SelfService.Infrastructure.Api.JsonSchema;

public class AddSelfServiceJsonSchemaRequest
{
    [Required]
    public JsonObject? Schema { get; set; } = null;
}
