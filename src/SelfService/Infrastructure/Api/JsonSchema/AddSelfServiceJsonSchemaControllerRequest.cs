using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.JsonSchema;

public class AddSelfServiceJsonSchemaRequest
{
    [Required]
    public string? Schema { get; set; }
}
