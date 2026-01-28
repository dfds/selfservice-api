using Microsoft.Extensions.Caching.Memory;

namespace SelfService.Infrastructure.Persistence;

class RbacCache
{
    private IMemoryCache _cache;

    public RbacCache()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    public async Task<T> GetOrAddAsync<T>(string prefix, string key, Func<Task<T>> dataFetch)
    {
        var formattedKey = $"{prefix}-{key}";
        if (_cache.TryGetValue(formattedKey, out var value))
        {
            return (T)value!;
        }

        var result = await dataFetch();
        _cache.Set(formattedKey, result);
        return result;
    }

    public void Reset()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }
}

class CacheConst
{
    public const string PermissionGrantsForUser = "PermissionGrantsForUser";
    public const string RoleGrantsForUser = "RoleGrantsForUser";
    public const string UserGroupPermissions = "UserGroupPermissions";
    public const string UserGroupRoles = "UserGroupRoles";
    public const string PermissionGrantsForRole = "PermissionGrantsForRole";
    public const string PermissionGrantsForRoleIgnoreCase = "PermissionGrantsForRoleIgnoreCase";
    public const string PermissionGrantsForGroup = "PermissionGrantsForGroup";
    public const string PermissionGrantsForCapability = "PermissionGrantsForCapability";
    public const string RoleGrantsForCapability = "RoleGrantsForCapability";
    public const string RoleGrantsForGroup = "RoleGrantsForGroup";
    public const string GroupsForUser = "GroupsForUser";
    public const string AssignableRoles = "AssignableRoles";
    public const string SystemGroups = "SystemGroups";
}
