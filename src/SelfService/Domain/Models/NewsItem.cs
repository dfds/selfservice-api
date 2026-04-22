namespace SelfService.Domain.Models;

public class NewsItem : Entity<NewsItemId>
{
    public string Title { get; private set; }
    public string Body { get; private set; }
    public DateTime DueDate { get; private set; }
    public bool IsHighlighted { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    public NewsItem(
        NewsItemId id,
        string title,
        string body,
        DateTime dueDate,
        bool isHighlighted,
        string createdBy,
        DateTime createdAt
    )
        : base(id)
    {
        Title = title;
        Body = body;
        DueDate = dueDate;
        IsHighlighted = isHighlighted;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public void Update(string? title, string? body, DateTime? dueDate, DateTime modifiedAt)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title;
        }

        if (!string.IsNullOrWhiteSpace(body))
        {
            Body = body;
        }

        if (dueDate.HasValue)
        {
            DueDate = dueDate.Value;
        }

        ModifiedAt = modifiedAt;
    }

    public void SetHighlighted(bool highlighted, DateTime modifiedAt)
    {
        IsHighlighted = highlighted;
        ModifiedAt = modifiedAt;
    }

    public bool IsRelevant()
    {
        return DueDate.Date >= DateTime.UtcNow.Date;
    }
}
