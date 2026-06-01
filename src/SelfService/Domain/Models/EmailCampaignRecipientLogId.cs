namespace SelfService.Domain.Models;

public class EmailCampaignRecipientLogId : ValueObjectGuid<EmailCampaignRecipientLogId>
{
    private EmailCampaignRecipientLogId(Guid newGuid)
        : base(newGuid) { }
}
