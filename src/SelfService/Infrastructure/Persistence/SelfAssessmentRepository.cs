using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Queries;

public class selfAssessmentRepository : ISelfAssessmentRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public selfAssessmentRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddSelfAssessment(SelfAssessment selfAssessment)
    {
        await _dbContext.SelfAssessments.AddAsync(selfAssessment);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> SelfAssessmentExists(CapabilityId capabilityId, string selfAssessmentType)
    {
        return await _dbContext.SelfAssessments.AnyAsync(
            c => c.CapabilityId == capabilityId && c.SelfAssessmentType == selfAssessmentType
        );
    }

    public async Task<List<SelfAssessment>> GetSelfAssessmentsForCapability(CapabilityId capabilityId)
    {
        return await _dbContext.SelfAssessments.Where(x => x.CapabilityId == capabilityId).ToListAsync();
    }

    public async Task<SelfAssessment?> GetSpecificSelfAssessmentForCapability(
        CapabilityId capabilityId,
        string selfAssessmentType
    )
    {
        return await _dbContext.SelfAssessments.FirstOrDefaultAsync(
            c => c.CapabilityId == capabilityId && c.SelfAssessmentType == selfAssessmentType
        );
    }

    public async Task RemoveSelfAssessment(SelfAssessment selfAssessment)
    {
        _dbContext.SelfAssessments.Remove(selfAssessment);
        await _dbContext.SaveChangesAsync();
    }

    /*
     * [2024-07-22] andfris: Temporary solution
     * The following assessment options should be stored in a database rather than in code.
     * This is a temporary solution to get the feature up and running quickly.
     * If the feature is to be kept, the assessment options should be moved to a database.
     */
    public List<SelfAssessmentOption> ListPossibleSelfAssessments()
    {
        return new List<SelfAssessmentOption>
        {
            new SelfAssessmentOption(selfAssessmentType: "snyk", description: "Code is monitored by Snyk"),
            new SelfAssessmentOption(
                selfAssessmentType: "grafana",
                description: "Capability is monitored using Grafana"
            ),
            new SelfAssessmentOption(selfAssessmentType: "finout", description: "Capability is monitored using Finout"),
        };
    }
}
