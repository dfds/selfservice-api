using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.News;

namespace SelfService.Domain.Services;

public class NewsItemService : INewsItemService
{
    private readonly ILogger<NewsItemService> _logger;
    private readonly INewsItemRepository _newsItemRepository;

    public NewsItemService(ILogger<NewsItemService> logger, INewsItemRepository newsItemRepository)
    {
        _logger = logger;
        _newsItemRepository = newsItemRepository;
    }

    public async Task<IEnumerable<NewsItem>> GetAllNewsItems()
    {
        return await _newsItemRepository.GetAll();
    }

    public async Task<NewsItem> GetNewsItemById(NewsItemId id)
    {
        return await _newsItemRepository.FindById(id)
            ?? throw new KeyNotFoundException($"News item with id '{id}' not found.");
    }

    [TransactionalBoundary]
    public async Task<NewsItem> CreateNewsItem(NewsItem newsItem)
    {
        await _newsItemRepository.Add(newsItem);
        return newsItem;
    }

    [TransactionalBoundary]
    public async Task UpdateNewsItem(NewsItemId id, NewsItemUpdateRequest updateRequest)
    {
        var newsItem =
            await _newsItemRepository.FindById(id)
            ?? throw new KeyNotFoundException($"News item with id '{id}' not found.");

        newsItem.Update(updateRequest.Title, updateRequest.Body, updateRequest.DueDate, DateTime.UtcNow);
    }

    [TransactionalBoundary]
    public async Task DeleteNewsItem(NewsItemId id)
    {
        await _newsItemRepository.Remove(id);
    }

    public async Task<List<NewsItem>> GetRelevantNewsItems()
    {
        return await _newsItemRepository.GetRelevantNewsItems();
    }

    [TransactionalBoundary]
    public async Task HighlightNewsItem(NewsItemId id)
    {
        var newsItem =
            await _newsItemRepository.FindById(id)
            ?? throw new KeyNotFoundException($"News item with id '{id}' not found.");

        // Toggle: if the item is already highlighted, just remove the highlight.
        if (newsItem.IsHighlighted)
        {
            newsItem.SetHighlighted(false, DateTime.UtcNow);
            return;
        }

        // Otherwise remove highlight from all others first, then highlight this one.
        await _newsItemRepository.ClearHighlight();

        newsItem.SetHighlighted(true, DateTime.UtcNow);
    }
}
