using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class MemberRepository : IMemberRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public MemberRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(Member member)
    {
        await _dbContext.Members.AddAsync(member);
    }

    public async Task<Member?> FindBy(UserId userId)
    {
        return await _dbContext.Members.FindAsync(userId);
    }
}
