using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Infrastructure.Persistence;

public class UserEmailQuery : IUserEmailQuery
{
    private readonly SelfServiceDbContext _dbContext;

    public UserEmailQuery(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<UserEmailInfo>> GetUsersWithFilters(
        IEnumerable<string>? roles,
        IEnumerable<string>? costCentres,
        IEnumerable<string>? businessCapabilities,
        IEnumerable<string>? capabilities
    )
    {
        var roleList = roles?.ToList() ?? new List<string>();
        var costCentreList = costCentres?.ToList() ?? new List<string>();
        var businessCapabilityList = businessCapabilities?.ToList() ?? new List<string>();
        var capabilityList = capabilities?.ToList() ?? new List<string>();

        // If no filters are provided, return all users
        if (!roleList.Any() && !costCentreList.Any() && !businessCapabilityList.Any() && !capabilityList.Any())
        {
            var allUsers = await _dbContext
                .Members.Select(m => new UserEmailInfo { Name = m.DisplayName ?? "", Email = m.Email })
                .Distinct()
                .ToListAsync();
            return allUsers;
        }

        // Step 1: Determine which capabilities we're interested in
        // Collect capability IDs from cost-centre, business-capability, and capability filters
        List<CapabilityId>? targetCapabilityIds = null;

        if (costCentreList.Any() || businessCapabilityList.Any() || capabilityList.Any())
        {
            var capabilitySets = new List<HashSet<CapabilityId>>();

            if (costCentreList.Any())
            {
                var capIds = await GetCapabilityIdsByMetadataField("dfds.cost.centre", costCentreList);
                capabilitySets.Add(capIds.ToHashSet());
            }

            if (businessCapabilityList.Any())
            {
                var capIds = await GetCapabilityIdsByMetadataField("dfds.businessCapability", businessCapabilityList);
                capabilitySets.Add(capIds.ToHashSet());
            }

            if (capabilityList.Any())
            {
                var capIds = await GetCapabilityIdsByCapabilityFilter(capabilityList);
                capabilitySets.Add(capIds.ToHashSet());
            }

            // Intersect capability sets - all capability filters must match
            if (capabilitySets.Count == 1)
            {
                targetCapabilityIds = capabilitySets[0].ToList();
            }
            else
            {
                targetCapabilityIds = capabilitySets
                    .Aggregate((a, b) => new HashSet<CapabilityId>(a.Intersect(b)))
                    .ToList();
            }
        }

        // Step 2: Get users based on role and capability filters
        HashSet<UserId> finalUserIds;

        if (roleList.Any())
        {
            // Get users with the role (global or in specific capabilities)
            var usersWithRole = await GetUserIdsByRbacRoles(roleList, targetCapabilityIds);

            // If we have target capabilities, we need to intersect with actual membership
            if (targetCapabilityIds != null && targetCapabilityIds.Any())
            {
                // Get users who are members of the target capabilities
                var membersOfTargetCapabilities = await _dbContext
                    .Memberships.Where(m => targetCapabilityIds.Contains(m.CapabilityId))
                    .Select(m => m.UserId)
                    .Distinct()
                    .ToListAsync();

                // Return intersection: users who have the role AND are members of target capabilities
                finalUserIds = usersWithRole.Intersect(membersOfTargetCapabilities.ToHashSet()).ToHashSet();
            }
            else
            {
                // No capability filters, just return users with the role
                finalUserIds = usersWithRole;
            }
        }
        else
        {
            // No role filter, get all members of target capabilities
            if (targetCapabilityIds != null && targetCapabilityIds.Any())
            {
                var userIdsList = await _dbContext
                    .Memberships.Where(m => targetCapabilityIds.Contains(m.CapabilityId))
                    .Select(m => m.UserId)
                    .Distinct()
                    .ToListAsync();
                finalUserIds = userIdsList.ToHashSet();
            }
            else
            {
                finalUserIds = new HashSet<UserId>();
            }
        }

        // Get members with the filtered user IDs
        var results = await _dbContext
            .Members.Where(m => finalUserIds.Contains(m.Id))
            .Select(m => new UserEmailInfo { Name = m.DisplayName ?? "", Email = m.Email })
            .Distinct()
            .ToListAsync();

        return results;
    }

    private async Task<HashSet<UserId>> GetUserIdsByRbacRoles(
        List<string> roleNames,
        List<CapabilityId>? targetCapabilityIds
    )
    {
        // Parse role names that might be IDs
        var parsedRoleIds = new List<RbacRoleId>();
        var nameFilters = new List<string>();

        foreach (var name in roleNames)
        {
            if (RbacRoleId.TryParse(name, out var roleId))
            {
                parsedRoleIds.Add(roleId);
            }
            else
            {
                nameFilters.Add(name.ToLower());
            }
        }

        // Find all RBAC roles with matching names (case-insensitive) or IDs
        var rbacRoles = await _dbContext
            .RbacRoles.Where(r => nameFilters.Any(nf => r.Name.ToLower() == nf) || parsedRoleIds.Contains(r.Id))
            .ToListAsync();

        var roleIds = rbacRoles.Select(r => r.Id).ToList();

        // Find all role grants for these roles assigned to users
        var roleGrantsQuery = _dbContext.RbacRoleGrants.Where(rg =>
            roleIds.Contains(rg.RoleId) && rg.AssignedEntityType == AssignedEntityType.User
        );

        var allRoleGrants = await roleGrantsQuery.ToListAsync();

        // If target capabilities are specified, filter role grants to those capabilities
        if (targetCapabilityIds != null && targetCapabilityIds.Any())
        {
            var capabilityIdStrings = targetCapabilityIds.Select(c => c.ToString()).ToList();

            // Filter grants that are either global or for the specified capabilities
            var filteredGrants = allRoleGrants
                .Where(rg =>
                    rg.Type == RbacAccessType.Global
                    || (
                        rg.Type == RbacAccessType.Capability
                        && rg.Resource != null
                        && capabilityIdStrings.Contains(rg.Resource)
                    )
                )
                .ToList();

            // Extract user IDs and parse them
            var userIds = new HashSet<UserId>();
            foreach (var rg in filteredGrants)
            {
                if (UserId.TryParse(rg.AssignedEntityId, out var userId))
                {
                    userIds.Add(userId);
                }
            }

            return userIds;
        }

        // No capability filter - return all users with the role
        var allUserIds = new HashSet<UserId>();
        foreach (var rg in allRoleGrants)
        {
            if (UserId.TryParse(rg.AssignedEntityId, out var userId))
            {
                allUserIds.Add(userId);
            }
        }

        return allUserIds;
    }

    private async Task<List<CapabilityId>> GetCapabilityIdsByMetadataField(string metadataKey, List<string> values)
    {
        // Get all active capabilities - don't filter on JsonMetadata in DB query
        // to avoid PostgreSQL JSON validation errors
        var capabilities = await _dbContext
            .Capabilities.Where(c => c.Status == CapabilityStatusOptions.Active)
            .ToListAsync();

        var matchingCapabilityIds = capabilities
            .Where(c =>
            {
                // Skip if JsonMetadata is null or empty
                if (string.IsNullOrWhiteSpace(c.JsonMetadata))
                    return false;

                try
                {
                    var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(c.JsonMetadata);
                    if (metadata != null && metadata.TryGetValue(metadataKey, out var metadataElement))
                    {
                        var metadataValue = metadataElement.GetString();
                        return values.Any(v => string.Equals(v, metadataValue, StringComparison.OrdinalIgnoreCase));
                    }
                }
                catch
                {
                    // Ignore invalid JSON or parsing errors
                }
                return false;
            })
            .Select(c => c.Id)
            .ToList();

        return matchingCapabilityIds;
    }

    private async Task<List<CapabilityId>> GetCapabilityIdsByCapabilityFilter(List<string> capabilityNames)
    {
        // Parse capability names that might be IDs
        var parsedCapabilityIds = new List<CapabilityId>();
        var nameFilters = new List<string>();

        foreach (var name in capabilityNames)
        {
            if (CapabilityId.TryParse(name, out var capabilityId))
            {
                parsedCapabilityIds.Add(capabilityId);
            }
            else
            {
                nameFilters.Add(name.ToLower());
            }
        }

        // Find capabilities by name (case-insensitive) or ID
        var capabilities = await _dbContext
            .Capabilities.Where(c =>
                c.Status == CapabilityStatusOptions.Active
                && (nameFilters.Any(nf => c.Name.ToLower() == nf) || parsedCapabilityIds.Contains(c.Id))
            )
            .ToListAsync();

        return capabilities.Select(c => c.Id).ToList();
    }
}
