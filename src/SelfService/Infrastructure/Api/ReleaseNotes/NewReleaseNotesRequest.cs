using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.ReleaseNotes;

public class NewReleaseNotesRequest
{
    [Required]
    public string? Title { get; set; }

    [Required]
    public DateTime ReleaseDate { get; set; }

    [Required]
    public string? Content { get; set; }

    public bool IsActive { get; set; }
}
