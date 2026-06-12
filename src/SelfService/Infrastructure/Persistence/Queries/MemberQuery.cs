using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Infrastructure.Persistence.Queries;

public class MemberQuery : IMemberQuery
{
    private readonly SelfServiceDbContext _dbContext;

    public MemberQuery(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(List<Member> Items, int Total)> Search(MemberType? type, string? search, int limit, int offset)
    {
        if (limit <= 0)
            limit = 50;
        if (limit > 200)
            limit = 200;
        if (offset < 0)
            offset = 0;

        var query = _dbContext.Members.AsQueryable();

        if (type.HasValue)
        {
            var t = type.Value;
            query = query.Where(m => m.Type == t);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim().ToLower();
            query = query.Where(m =>
                m.Email.ToLower().Contains(needle)
                || (m.DisplayName != null && m.DisplayName.ToLower().Contains(needle))
            );
        }

        var total = await query.CountAsync();
        var items = await query.OrderBy(m => m.Email).Skip(offset).Take(limit).ToListAsync();

        return (items, total);
    }
}
