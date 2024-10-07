using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Queries;

public class SelfAssessmentOptionRepository : ISelfAssessmentOptionRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public SelfAssessmentOptionRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddSelfAssessmentOption(SelfAssessmentOption option)
    {
        await _dbContext.SelfAssessmentOptions.AddAsync(option);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<SelfAssessmentOption>> GetAllSelfAssessmentOptions()
    {
        return await _dbContext.SelfAssessmentOptions.ToListAsync();
    }

    public async Task<List<SelfAssessmentOption>> GetActiveSelfAssessmentOptions()
    {
        return await _dbContext.SelfAssessmentOptions.Where(o => o.IsActive).ToListAsync();
    }

    public async Task<SelfAssessmentOption?> Get(SelfAssessmentOptionId id)
    {
        return await _dbContext.SelfAssessmentOptions.FindAsync(id);
    }

    public async Task DeactivateSelfAssessmentOption(SelfAssessmentOptionId id)
    {
        var option = await _dbContext.SelfAssessmentOptions.FindAsync(id);
        if (option is not null)
        {
            option.Deactivate();
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task ActivateSelfAssessmentOption(SelfAssessmentOptionId id)
    {
        var option = await _dbContext.SelfAssessmentOptions.FindAsync(id);
        if (option is not null)
        {
            option.Activate();
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task UpdateSelfAssessmentOption(
        SelfAssessmentOptionId id,
        string? shortName,
        string? description,
        string? documentationUrl
    )
    {
        var option = await _dbContext.SelfAssessmentOptions.FindAsync(id);
        if (option is not null)
        {
            option.Update(shortName, description, documentationUrl);
            await _dbContext.SaveChangesAsync();
        }
    }
}
