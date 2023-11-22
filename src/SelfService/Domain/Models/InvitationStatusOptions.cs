namespace SelfService.Domain.Models;

public class InvitationStatusOptions : ValueObjectEnum<InvitationStatusOptions>
{
    public static readonly InvitationStatusOptions Unknown = new("Unknown");
    public static readonly InvitationStatusOptions Active = new("Active");
    public static readonly InvitationStatusOptions Declined = new("Declined");
    public static readonly InvitationStatusOptions Accepted = new("Accepted");
    public static readonly InvitationStatusOptions Cancelled = new("Cancelled");

    private InvitationStatusOptions(string value)
        : base(value) { }
}
