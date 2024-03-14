using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class UserAction : IDomainEvent
{
    public string? Action { get; set; }
    public string? Path { get; set; }
    public string? Method { get; set; }
    public long? Timestamp { get; set; }
    public string? Username { get; set; }
    public string? Service { get; set; }
    public string? RequestData { get; set; }

    public const string EventType = "user-action";
}
