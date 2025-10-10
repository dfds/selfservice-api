using Microsoft.EntityFrameworkCore;
using SelfService.Domain;
using SelfService.Domain.Exceptions;
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

    public async Task<Member> Remove(UserId userId)
    {
        var member = await _dbContext.Members.FindAsync(userId);

        if (member is null)
        {
            throw EntityNotFoundException<Member>.UsingId(userId);
        }

        _dbContext.Members.Remove(member);

        return member;
    }

    [TransactionalBoundary]
    public async Task<Member> Update(Member member)
    {
        var existingMember = await _dbContext.Members.FindAsync(member.Id);

        if (existingMember is null)
        {
            throw EntityNotFoundException<Member>.UsingId(member.Id);
        }

        _dbContext.Members.Update(member);

        return member;
    }

    public Task<List<Member>> GetAll()
    {
        return _dbContext.Members.ToListAsync();
    }
}
