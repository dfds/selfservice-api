using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class EmailCampaignRecipientLogRepository : IEmailCampaignRecipientLogRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public EmailCampaignRecipientLogRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(EmailCampaignRecipientLog log)
    {
        await _dbContext.EmailCampaignRecipientLogs.AddAsync(log);
    }

    public async Task AddRange(IEnumerable<EmailCampaignRecipientLog> logs)
    {
        await _dbContext.EmailCampaignRecipientLogs.AddRangeAsync(logs);
    }

    public async Task<List<EmailCampaignRecipientLog>> GetByCampaignId(EmailCampaignId campaignId)
    {
        return await _dbContext
            .EmailCampaignRecipientLogs.Where(l => l.EmailCampaignId == campaignId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<EmailCampaignRecipientLog>> GetByExecutionId(EmailCampaignExecutionId executionId)
    {
        return await _dbContext
            .EmailCampaignRecipientLogs.Where(l => l.ExecutionId == executionId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task<EmailCampaignRecipientLog?> FindById(EmailCampaignRecipientLogId id)
    {
        return await _dbContext.EmailCampaignRecipientLogs.FindAsync(id);
    }

    public async Task<List<EmailCampaignRecipientLog>> GetFailedByCampaignId(EmailCampaignId campaignId)
    {
        return await _dbContext
            .EmailCampaignRecipientLogs.Where(l =>
                l.EmailCampaignId == campaignId && l.Status == EmailCampaignRecipientStatus.Failed
            )
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();
    }
}
