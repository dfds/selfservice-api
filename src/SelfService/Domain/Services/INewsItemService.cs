using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.News;

namespace SelfService.Domain.Services;

public interface INewsItemService
{
    Task<IEnumerable<NewsItem>> GetAllNewsItems();
    Task<NewsItem> GetNewsItemById(NewsItemId id);
    Task<NewsItem> CreateNewsItem(NewsItem newsItem);
    Task UpdateNewsItem(NewsItemId id, NewsItemUpdateRequest updateRequest);
    Task DeleteNewsItem(NewsItemId id);
    Task<List<NewsItem>> GetRelevantNewsItems();
    Task HighlightNewsItem(NewsItemId id);
}
