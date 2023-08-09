using Dafda.Consuming;
using SelfService.Application;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Messaging.Legacy;

public class AwsContextAccountCreated
{
    public const string EventType = "aws_context_account_created";

    public string? ContextId { get; set; }
    public string? CapabilityId { get; set; }
    public string? CapabilityName { get; set; }
    public string? CapabilityRootId { get; set; }
    public string? ContextName { get; set; }
    public string? AccountId { get; set; }
    public string? RoleArn { get; set; }
    public string? RoleEmail { get; set; }
}

public class AwsContextAccountCreatedHandler : IMessageHandler<AwsContextAccountCreated>
{
    private readonly IAwsAccountApplicationService _awsAccountApplicationService;

    public AwsContextAccountCreatedHandler(IAwsAccountApplicationService awsAccountApplicationService)
    {
        _awsAccountApplicationService = awsAccountApplicationService;
    }

    public async Task Handle(AwsContextAccountCreated message, MessageHandlerContext context)
    {
        if (!AwsAccountId.TryParse(message.ContextId, out var id))
        {
            throw new InvalidOperationException($"Invalid AwsAccountId {message.ContextId}");
        }
        if (!RealAwsAccountId.TryParse(message.AccountId, out var realAwsAccountId))
        {
            throw new InvalidOperationException($"Invalid RealAwsAccountId {message.AccountId}");
        }

        await _awsAccountApplicationService.RegisterRealAwsAccount(id, realAwsAccountId, message.RoleEmail);
    }
}
