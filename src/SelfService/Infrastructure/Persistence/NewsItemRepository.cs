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
        return await DbSetReference
            .Where(n => n.DueDate >= today)
            .OrderBy(n => n.DueDate)
            .ToListAsync();
    }

    public async Task ClearHighlight()
    {
        var highlighted = await DbSetReference.Where(n => n.IsHighlighted).ToListAsync();
        foreach (var item in highlighted)
        {
            item.SetHighlighted(false, DateTime.UtcNow);
        }
    }
}
