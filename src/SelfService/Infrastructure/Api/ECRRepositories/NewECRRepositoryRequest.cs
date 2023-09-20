using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.ECRRepositories;

public class NewECRRepositoryRequest
{
    [Required]
    public string? Name { get; set; }

    [Required]
    public string? Description { get; set; }

    [Required]
    public string? RepositoryName { get; set; }
}
