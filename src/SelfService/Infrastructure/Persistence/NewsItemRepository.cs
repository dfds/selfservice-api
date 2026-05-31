using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class NewsItemRepository : GenericRepository<NewsItem, NewsItemId>, INewsItemRepository
{
    public NewsItemRepository(SelfServiceDbContext dbContext)
        : base(dbContext.NewsItems) { }

    public async Task<List<NewsItem>> GetRelevantNewsItems()
    {
        var today = DateTime.UtcNow.Date;
        return await DbSetReference.Where(n => n.DueDate >= today).OrderBy(n => n.DueDate).ToListAsync();
    }

    public async Task ClearHighlight()
    {
        // Execute the clear as its own statement so the previously highlighted row is set to
        // false in the database before a new row is set to true. Staging both changes through
        // the change tracker lets EF batch them into a single SaveChanges, where it may emit the
        // set-true UPDATE before the set-false one and transiently violate the
        // "UX_NewsItem_Highlighted" partial unique index (only one highlighted row allowed).
        await DbSetReference
            .Where(n => n.IsHighlighted)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(n => n.IsHighlighted, false).SetProperty(n => n.ModifiedAt, DateTime.UtcNow)
            );
    }
}
