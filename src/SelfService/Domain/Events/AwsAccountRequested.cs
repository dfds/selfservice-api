using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class AwsAccountRequested : IDomainEvent
{
    public const string EventType = "aws-account-requested";

    public string? AccountId { get; set; }
}