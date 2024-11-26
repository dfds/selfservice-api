using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Queries;

public class SelfAssessmentRepository : ISelfAssessmentRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public SelfAssessmentRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task UpdateSelfAssessment(SelfAssessment selfAssessment)
    {
        await _dbContext.SelfAssessments.AddAsync(selfAssessment);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> SelfAssessmentExists(
        CapabilityId capabilityId,
        SelfAssessmentOptionId selfAssessmentOptionId
    )
    {
        return await _dbContext.SelfAssessments.AnyAsync(
            a => a.CapabilityId == capabilityId && a.OptionId == selfAssessmentOptionId
        );
    }

    public async Task<List<SelfAssessment>> GetSelfAssessmentsForCapability(CapabilityId capabilityId)
    {
        return await _dbContext.SelfAssessments.Where(a => a.CapabilityId == capabilityId).ToListAsync();
    }

    public async Task<SelfAssessment?> GetSpecificSelfAssessmentForCapability(
        CapabilityId capabilityId,
        SelfAssessmentOptionId selfAssessmentOptionId
    )
    {
        return await _dbContext.SelfAssessments.FirstOrDefaultAsync(
            a => a.CapabilityId == capabilityId && a.OptionId == selfAssessmentOptionId
        );
    }
}
