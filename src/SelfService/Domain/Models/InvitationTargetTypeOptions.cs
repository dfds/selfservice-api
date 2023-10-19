namespace SelfService.Domain.Models;

public class InvitationTargetTypeOptions : ValueObjectEnum<InvitationTargetTypeOptions>
{
    public static readonly InvitationTargetTypeOptions Unknown = new("Unknown");
    public static readonly InvitationTargetTypeOptions Capability = new("Capability");

    private InvitationTargetTypeOptions(string value)
        : base(value) { }
}
