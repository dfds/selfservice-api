using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Capabilities;

public class NewAzureResourceRequest
{
    [Required]
    public string? environment { get; set; } = null;
}
