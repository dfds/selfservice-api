using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.ECSRepositories;

public class NewECSRepositoryRequest
{
    [Required]
    public string? Name { get; set; }

    [Required]
    public string? Description { get; set; }

    [Required]
    public string? RepositoryName { get; set; }
}
