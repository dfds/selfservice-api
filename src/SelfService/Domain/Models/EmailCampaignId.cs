namespace SelfService.Domain.Models;

public class EmailCampaignId : ValueObjectGuid<EmailCampaignId>
{
    private EmailCampaignId(Guid newGuid)
        : base(newGuid) { }
}
