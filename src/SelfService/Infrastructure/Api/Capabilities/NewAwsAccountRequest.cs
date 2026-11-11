using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Capabilities;

public class NewAwsAccountRequest
{
    [Required]
    public string? environment { get; set; } = null;
}
