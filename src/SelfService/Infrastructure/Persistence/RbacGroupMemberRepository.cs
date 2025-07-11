using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class RbacGroupMemberRepository : GenericRepository<RbacGroupMember, RbacGroupMemberId>, IRbacGroupMemberRepository
{
    public RbacGroupMemberRepository(SelfServiceDbContext dbContext) : base(dbContext.RbacGroupMembers)
    {
        
    }
}