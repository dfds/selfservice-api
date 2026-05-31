using Microsoft.Extensions.Logging;
using Moq;
using SelfService.Domain.Models;
using SelfService.Domain.Services;

namespace SelfService.Tests.Domain.Services;

public class TestNewsItemService
{
    private static NewsItem BuildNewsItem(bool isHighlighted)
    {
        return new NewsItem(
            id: new NewsItemId(),
            title: "Title",
            body: "Body",
            dueDate: DateTime.UtcNow.AddDays(1),
            isHighlighted: isHighlighted,
            createdBy: "tester",
            createdAt: DateTime.UtcNow
        );
    }

    [Fact]
    public async Task highlighting_a_not_highlighted_item_clears_others_and_highlights_it()
    {
        var newsItem = BuildNewsItem(isHighlighted: false);
        var repository = new Mock<INewsItemRepository>();
        repository.Setup(x => x.FindById(newsItem.Id)).ReturnsAsync(newsItem);

        var sut = new NewsItemService(Mock.Of<ILogger<NewsItemService>>(), repository.Object);

        await sut.HighlightNewsItem(newsItem.Id);

        Assert.True(newsItem.IsHighlighted);
        repository.Verify(x => x.ClearHighlight(), Times.Once);
    }

    [Fact]
    public async Task highlighting_an_already_highlighted_item_removes_the_highlight()
    {
        var newsItem = BuildNewsItem(isHighlighted: true);
        var repository = new Mock<INewsItemRepository>();
        repository.Setup(x => x.FindById(newsItem.Id)).ReturnsAsync(newsItem);

        var sut = new NewsItemService(Mock.Of<ILogger<NewsItemService>>(), repository.Object);

        await sut.HighlightNewsItem(newsItem.Id);

        Assert.False(newsItem.IsHighlighted);
        // Toggling off must not wipe other items' highlights.
        repository.Verify(x => x.ClearHighlight(), Times.Never);
    }

    [Fact]
    public async Task highlighting_a_missing_item_throws()
    {
        var missingId = new NewsItemId();
        var repository = new Mock<INewsItemRepository>();
        repository.Setup(x => x.FindById(missingId)).ReturnsAsync((NewsItem?)null);

        var sut = new NewsItemService(Mock.Of<ILogger<NewsItemService>>(), repository.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.HighlightNewsItem(missingId));
    }
}
