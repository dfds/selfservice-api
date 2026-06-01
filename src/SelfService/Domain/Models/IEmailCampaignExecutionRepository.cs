namespace SelfService.Domain.Models;

public interface IEmailCampaignExecutionRepository
{
    Task Add(EmailCampaignExecution execution);
    Task<EmailCampaignExecution?> FindById(EmailCampaignExecutionId id);
    Task<List<EmailCampaignExecution>> GetByCampaignId(EmailCampaignId campaignId);
    Task<EmailCampaignExecution?> GetLatestByCampaignId(EmailCampaignId campaignId);
}
