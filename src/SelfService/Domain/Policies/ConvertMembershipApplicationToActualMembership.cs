using Dafda.Consuming;
using SelfService.Application;
using SelfService.Domain.Events;
using SelfService.Domain.Models;

namespace SelfService.Domain.Policies;

public class ConvertMembershipApplicationToActualMembership : IMessageHandler<MembershipApplicationHasBeenFinalized>
{
    private readonly ILogger<ConvertMembershipApplicationToActualMembership> _logger;
    private readonly IMembershipApplicationService _membershipApplicationService;

    public ConvertMembershipApplicationToActualMembership(ILogger<ConvertMembershipApplicationToActualMembership> logger, IMembershipApplicationService membershipApplicationService)
    {
        _logger = logger;
        _membershipApplicationService = membershipApplicationService;
    }

    public async Task Handle(MembershipApplicationHasBeenFinalized message, MessageHandlerContext context)
    {
        using var _ = _logger.BeginScope("Handling {MessageType} on {ImplementationType} with {CorrelationId} and {CausationId}",
            context.MessageType, GetType().Name, context.CorrelationId, context.CausationId);

        if (!MembershipApplicationId.TryParse(message.MembershipApplicationId, out var membershipApplicationId))
        {
            _logger.LogWarning("Cannot try to finalize membership application because membership application id \"{MembershipApplicationId}\" is not valid - skipping message {MessageId}",
                message.MembershipApplicationId, context.MessageId);

            return;
        }

        _logger.LogDebug("Accepting membership application {MembershipApplicationId}", membershipApplicationId);
        await _membershipApplicationService.AcceptApplication(membershipApplicationId);
    }
}