namespace SelfService.Domain.Models;

public interface IEmailCampaignRecipientLogRepository
{
    Task Add(EmailCampaignRecipientLog log);
    Task AddRange(IEnumerable<EmailCampaignRecipientLog> logs);
    Task<List<EmailCampaignRecipientLog>> GetByCampaignId(EmailCampaignId campaignId);
    Task<List<EmailCampaignRecipientLog>> GetByExecutionId(EmailCampaignExecutionId executionId);
    Task<EmailCampaignRecipientLog?> FindById(EmailCampaignRecipientLogId id);
    Task<List<EmailCampaignRecipientLog>> GetFailedByCampaignId(EmailCampaignId campaignId);
}
