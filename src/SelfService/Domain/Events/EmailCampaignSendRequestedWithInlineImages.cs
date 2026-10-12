using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public sealed class EmailCampaignSendRequestedWithInlineImages : IDomainEvent
{
    public const string EventType = "email-send-requested-with-inline-images";

    public string? EmailSendId { get; set; }
    public string? RecipientLogId { get; set; }
    public string? RecipientEmail { get; set; }
    public string? Subject { get; set; }
    public string? HtmlBody { get; set; }
    public List<InlineEmailImage> InlineImages { get; set; } = [];
}

public sealed record InlineEmailImage(string ContentId, string ObjectKey, string ContentType, string FileName);