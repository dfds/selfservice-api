using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class EmailCampaignExecutionRepository : IEmailCampaignExecutionRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public EmailCampaignExecutionRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(EmailCampaignExecution execution)
    {
        await _dbContext.EmailCampaignExecutions.AddAsync(execution);
    }

    public async Task<EmailCampaignExecution?> FindById(EmailCampaignExecutionId id)
    {
        return await _dbContext.EmailCampaignExecutions.FindAsync(id);
    }

    public async Task<List<EmailCampaignExecution>> GetByCampaignId(EmailCampaignId campaignId)
    {
        return await _dbContext
            .EmailCampaignExecutions.Where(e => e.EmailCampaignId == campaignId)
            .OrderByDescending(e => e.ExecutedAt)
            .ToListAsync();
    }

    public async Task<EmailCampaignExecution?> GetLatestByCampaignId(EmailCampaignId campaignId)
    {
        return await _dbContext
            .EmailCampaignExecutions.Where(e => e.EmailCampaignId == campaignId)
            .OrderByDescending(e => e.ExecutedAt)
            .FirstOrDefaultAsync();
    }
}
