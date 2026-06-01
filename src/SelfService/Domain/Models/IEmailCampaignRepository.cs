namespace SelfService.Domain.Models;

public interface IEmailCampaignRepository
{
    Task Add(EmailCampaign campaign);
    Task<EmailCampaign?> FindById(EmailCampaignId id);
    Task<List<EmailCampaign>> GetAll();
    Task<List<EmailCampaign>> GetByStatus(EmailCampaignStatus status);
    Task Remove(EmailCampaign campaign);
    Task<List<EmailCampaign>> GetDueScheduled();
    Task<List<EmailCampaign>> GetDueRecurring();
}
