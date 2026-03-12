using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class EmailCampaignRepository : IEmailCampaignRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public EmailCampaignRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(EmailCampaign campaign)
    {
        await _dbContext.EmailCampaigns.AddAsync(campaign);
    }

    public async Task<EmailCampaign?> FindById(EmailCampaignId id)
    {
        var campaign = await _dbContext.EmailCampaigns.FindAsync(id);
        if (campaign is { IsDeleted: true })
            return null;
        return campaign;
    }

    public async Task<List<EmailCampaign>> GetAll()
    {
        return await _dbContext.EmailCampaigns
            .Where(b => !b.IsDeleted)
            .OrderByDescending(b => b.ModifiedAt)
            .ToListAsync();
    }

    public async Task<List<EmailCampaign>> GetByStatus(EmailCampaignStatus status)
    {
        return await _dbContext
            .EmailCampaigns.Where(b => !b.IsDeleted && b.Status == status)
            .OrderByDescending(b => b.ModifiedAt)
            .ToListAsync();
    }

    public Task Remove(EmailCampaign campaign)
    {
        _dbContext.EmailCampaigns.Remove(campaign);
        return Task.CompletedTask;
    }

    public async Task<List<EmailCampaign>> GetDueScheduled()
    {
        return await _dbContext.EmailCampaigns
            .Where(b =>
                !b.IsDeleted
                && b.Status == EmailCampaignStatus.Scheduled
                && b.ScheduleType == EmailCampaignScheduleType.Scheduled
                && b.ScheduledAt != null
                && b.ScheduledAt <= DateTime.UtcNow
            )
            .ToListAsync();
    }

    public async Task<List<EmailCampaign>> GetDueRecurring()
    {
        return await _dbContext.EmailCampaigns
            .Where(b =>
                !b.IsDeleted
                && b.Status == EmailCampaignStatus.Scheduled
                && b.ScheduleType == EmailCampaignScheduleType.Recurring
                && b.CronExpression != null
            )
            .ToListAsync();
    }
}
