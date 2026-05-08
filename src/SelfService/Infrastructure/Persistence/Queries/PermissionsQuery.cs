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
        var query = _dbContext
            .RbacGroupMembers.Where(gm => gm.UserId == userId)
            .Join(
                _dbContext.RbacPermissionGrants.Where(pg => pg.AssignedEntityType == AssignedEntityType.Group),
                gm => Cast.CastAsText(gm.GroupId).ToLower(),
                pg => pg.AssignedEntityId.ToLower(),
                (gm, pg) => pg
            );
        return await query.ToListAsync();
    }

    public async Task<IList<RbacRoleGrant>> FindUserGroupRolesByUserId(string userId)
    {
        var query = _dbContext
            .RbacGroupMembers.Where(gm => gm.UserId == userId)
            .Join(
                _dbContext.RbacRoleGrants.Where(pg => pg.AssignedEntityType == AssignedEntityType.Group),
                gm => Cast.CastAsText(gm.GroupId).ToLower(),
                pg => pg.AssignedEntityId.ToLower(),
                (gm, pg) => pg
            );

        return await query.ToListAsync();
    }

    public async Task<List<RbacPermissionGrant>> FindGuestPermissions()
    {
        var guestRole = await _dbContext.RbacRoles.FirstOrDefaultAsync(r => r.Name.ToLower() == "guest");

        if (guestRole == null)
            return new List<RbacPermissionGrant>();

        var guestRoleId = guestRole.Id.ToString();

        return await _dbContext
            .RbacPermissionGrants.Where(pg =>
                pg.AssignedEntityType == AssignedEntityType.Role
                && pg.AssignedEntityId.ToLower() == guestRoleId.ToLower()
            )
            .ToListAsync();
    }
}
