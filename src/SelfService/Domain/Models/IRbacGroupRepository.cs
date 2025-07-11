namespace SelfService.Domain.Models;

public interface IRbacGroupRepository : IGenericRepository<RbacGroup, RbacGroupId>
{
    Task<List<RbacGroup>> GetAllGroupsForUserId(string userId);
}