using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.SelfServiceJsonSchema;

public class AddSelfServiceJsonSchemaRequest
{
    [Required]
    public string? ObjectId { get; set; }

    [Required]
    public string? Schema { get; set; }
}
