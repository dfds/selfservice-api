using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Invitations;

public class InvitationsRequest
{
    [Required]
    public List<string> Invitees { get; set; } = new();
}
