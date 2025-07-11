using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Persistence.Functions;

namespace SelfService.Infrastructure.Persistence.Queries;

public class PermissionsQuery : IPermissionQuery
{
    private readonly SelfServiceDbContext _dbContext;

    public PermissionsQuery(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IList<RbacPermissionGrant>> FindUserGroupPermissionsByUserId(string userId)
    {
        // var query = from groupMember in _dbContext.RbacGroupMembers
        //     join permissionGrant in _dbContext.RbacPermissionGrants on groupMember.GroupId equals permissionGrant.AssignedEntityId
        //     where groupMember.UserId == userId
        //         select permissionGrant;
        
        var queryMeh = _dbContext.RbacGroupMembers
            .Where(gm => gm.UserId == userId)
            .SelectMany(gm => _dbContext.RbacPermissionGrants
            .Where(pg => pg.AssignedEntityType == AssignedEntityType.Group && gm.GroupId == pg.AssignedEntityId.ToLower())
            );

        var queryMeh2 = _dbContext.RbacGroupMembers
            .Where(gm => gm.UserId == userId)
            .Join(
                _dbContext.RbacPermissionGrants.Where(pg => pg.AssignedEntityType == AssignedEntityType.Group),
                gm => Cast.CastAsText(gm.GroupId).ToLower(),
                pg => pg.AssignedEntityId.ToLower(),
                (gm, pg) => pg
            );
        var ree = queryMeh2.ToQueryString();
        // var queryRaw = _dbContext.RbacPermissionGrants.FromSqlInterpolated($@"
        // SELECT
        //     pg.*
        // FROM
        //     ""RbacGroupMember"" AS gm
        // JOIN
        //     ""RbacPermissionGrants"" AS pg ON CAST(gm.""GroupId"" AS TEXT) = LOWER(pg.""AssignedEntityId"")
        // WHERE
        //     pg.""AssignedEntityType"" = 'Group' AND gm.""UserId"" = {userId}
        // ");
            
        return await queryMeh2.ToListAsync();
    }
    
    public async Task<IList<RbacRoleGrant>> FindUserGroupRolesByUserId(string userId)
    {
        var query = _dbContext.RbacGroupMembers
            .Where(gm => gm.UserId == userId)
            .Join(
                _dbContext.RbacRoleGrants.Where(pg => pg.AssignedEntityType == AssignedEntityType.Group),
                gm => gm.GroupId,
                pg => pg.AssignedEntityId.ToLower(),
                (gm, pg) => pg
            );
            
        return await query.ToListAsync();
    }
}