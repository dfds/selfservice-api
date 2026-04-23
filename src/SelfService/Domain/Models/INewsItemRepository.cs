namespace SelfService.Domain.Models;

public interface INewsItemRepository : IGenericRepository<NewsItem, NewsItemId>
{
    Task<List<NewsItem>> GetRelevantNewsItems();
    Task ClearHighlight();
}
