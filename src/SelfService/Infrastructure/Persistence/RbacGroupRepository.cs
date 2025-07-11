using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class RbacGroupRepository : GenericRepository<RbacGroup, RbacGroupId>, IRbacGroupRepository
{
    protected readonly DbSet<RbacGroupMember> _groupMembersSet;
    protected readonly DbSet<RbacRoleGrant> _roleGrantsSet;
    protected readonly DbSet<RbacPermissionGrant> _permissionGrantsSet;
        
    public RbacGroupRepository(SelfServiceDbContext dbContext) : base(dbContext.RbacGroups)
    {
        _groupMembersSet = dbContext.RbacGroupMembers;
        _roleGrantsSet = dbContext.RbacRoleGrants;
        _permissionGrantsSet = dbContext.RbacPermissionGrants;
    }

    public async Task<List<RbacGroup>> GetAllGroupsForUserId(string userId)
    {
        var groupIdsQuery = from gm in _groupMembersSet
            where gm.UserId == userId
            select gm.GroupId;
        
        // var roleIdsQuery = from rg in _roleGrantsSet
        //     where groupIdsQuery.Contains(rg.)
        //     select rg.RoleId;

        var permissions = await (from pg in _permissionGrantsSet
                where groupIdsQuery.Any(q => q.ToString() == pg.AssignedEntityId)
                select pg) // Assuming 'Permission' is a string property
            .ToListAsync();
        
        // this.DbSetReference.Where(g => g.Members.Any(m => m.UserId.Equals(userId.ToString())));
        return await GetAllWithPredicate(p => p.Members.Any(m => m.UserId.Equals(userId.ToString())));
    }
}