namespace SelfService.Domain.Models;

public class EmailCampaignExecutionId : ValueObjectGuid<EmailCampaignExecutionId>
{
    private EmailCampaignExecutionId(Guid newGuid)
        : base(newGuid) { }
}
