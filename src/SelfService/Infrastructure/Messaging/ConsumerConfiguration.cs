using Dafda.Configuration;
using Dafda.Consuming;
using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Events;
using SelfService.Domain.Models;
using SelfService.Domain.Policies;

namespace SelfService.Infrastructure.Messaging;

public static class ConsumerConfiguration
{
    private const string SelfServicePrefix = "cloudengineering.selfservice";
    private const string ConfluentGatewayPrefix = "cloudengineering.confluentgateway";

    public static void AddMessaging(this WebApplicationBuilder builder)
    {
        builder.Services.AddOutbox(options =>
        {
            options.WithOutboxEntryRepository<OutboxEntryRepository>();
            options
                .ForTopic($"{SelfServicePrefix}.capability")
                .Register<CapabilityCreated>(
                    messageType: CapabilityCreated.EventType,
                    keySelector: x => x.CapabilityId
                )
                ;

            options
                .ForTopic($"{SelfServicePrefix}.awsaccount")
                .Register<AwsAccountRequested>(
                    messageType: AwsAccountRequested.EventType,
                    keySelector: x => x.AccountId
                )
                ;

            options
                .ForTopic($"{SelfServicePrefix}.membership")
                .Register<UserHasJoinedCapability>(
                    messageType: "user-has-joined-capability",
                    keySelector: x => x.UserId!
                )
                .Register<UserHasLeftCapability>(
                    messageType: "user-has-left-capability",
                    keySelector: x => x.UserId!
                )
                ;

            options
                .ForTopic($"{SelfServicePrefix}.kafkatopic")
                .Register<NewKafkaTopicHasBeenRequested>(
                    messageType: "topic-requested",
                    keySelector: x => x.KafkaTopicId!
                )
                ;

            options
                .ForTopic($"{SelfServicePrefix}.messagecontract")
                .Register<NewMessageContractHasBeenRequested>(
                    messageType: "message-contract-requested",
                    keySelector: x => x.MessageContractId!
                )
                .Register<NewMessageContractHasBeenProvisioned>(
                    messageType: "message-contract-provisioned",
                    keySelector: x => x.MessageContractId!
                )
                ;

            options
                .ForTopic($"{SelfServicePrefix}.membershipapplication")
                .Register<NewMembershipApplicationHasBeenSubmitted>(
                    messageType: "membership-application-submitted",
                    keySelector: x => x.MembershipApplicationId!
                )
                .Register<MembershipApplicationHasBeenFinalized>(
                    messageType: "membership-application-finalized",
                    keySelector: x => x.MembershipApplicationId!
                )
                .Register<MembershipApplicationHasReceivedAnApproval>(
                    messageType: "membership-application-received-approval",
                    keySelector: x => x.MembershipApplicationId!
                )
                .Register<MembershipApplicationHasBeenCancelled>(
                    messageType: "membership-application-cancelled",
                    keySelector: x => x.MembershipApplicationId!
                )
                ;

            //options
            //    .ForTopic($"{SelfServicePrefix}.portalvisit")
            //    .Register<NewPortalVisitRegistered>(
            //        messageType: "new-portal-visit-registered",
            //        keySelector: x => x.VisitedBy!
            //    )
            //    ;
        });
        
        builder.Services.AddConsumer(options =>
        {
            options.WithConfigurationSource(builder.Configuration);
            options.WithEnvironmentStyle("DEFAULT_KAFKA");

            var topic = builder.Configuration["SS_APISPECS_TOPIC"];
            options.RegisterMessageHandler<Placeholder, PlaceholderHandler>(topic, Placeholder.EventType);

            options
                .ForTopic($"{SelfServicePrefix}.capability")
                .RegisterMessageHandler<CapabilityCreated, CapabilityCreatedHandler>(CapabilityCreated.EventType);

            options
                .ForTopic($"{SelfServicePrefix}.awsaccount")
                .RegisterMessageHandler<AwsAccountRequested, AwsAccountRequestedHandler>(AwsAccountRequested.EventType);

            options
                .ForTopic($"{SelfServicePrefix}.kafkatopic")
                .Ignore("topic-requested")
                ;

            options
                .ForTopic($"{SelfServicePrefix}.membershipapplication")
                .Ignore("membership-application-submitted")
                .RegisterMessageHandler<MembershipApplicationHasBeenFinalized, ConvertMembershipApplicationToActualMembership>("membership-application-finalized")
                .RegisterMessageHandler<MembershipApplicationHasReceivedAnApproval, FinalizeMembershipApplication>("membership-application-received-approval")
                .RegisterMessageHandler<MembershipApplicationHasBeenCancelled, RemoveCancelledMembershipApplication>("membership-application-cancelled")
                ;

            #region confluent gateway events

            options
                .ForTopic($"{ConfluentGatewayPrefix}.schema")
                .RegisterMessageHandler<SchemaRegistered, MarkMessageContractAsProvisioned>("schema-registered")
                .RegisterMessageHandler<SchemaRegistered, MarkMessageContractAsProvisioned>("schema_registered") // NOTE [jandr@2023-03-29]: double registration due to underscore/dash confusion - we should be using dashes
                .Ignore("schema-registration-failed")
                ;
            options
                .ForTopic($"{ConfluentGatewayPrefix}.provisioning")
                .RegisterMessageHandler<KafkaTopicProvisioningHasBegun, UpdateKafkaTopicProvisioningProgress>("topic-provisioning-begun")
                .RegisterMessageHandler<KafkaTopicProvisioningHasBegun, UpdateKafkaTopicProvisioningProgress>("topic_provisioning_begun") // NOTE [jandr@2023-03-29]: double registration due to underscore/dash confusion - we should be using dashes
                .RegisterMessageHandler<KafkaTopicProvisioningHasCompleted, UpdateKafkaTopicProvisioningProgress>("topic-provisioned")
                .RegisterMessageHandler<KafkaTopicProvisioningHasCompleted, UpdateKafkaTopicProvisioningProgress>("topic_provisioned") // NOTE [jandr@2023-03-29]: double registration due to underscore/dash confusion - we should be using dashes
                ;

            #endregion

        });
    }
}

public class KafkaTopicProvisioningHasBegun
{
    public string? ClusterId { get; set; }
    public string? TopicId { get; set; }
    public string? TopicName { get; set; }
}

public class KafkaTopicProvisioningHasCompleted
{
    public string? ClusterId { get; set; }
    public string? TopicId { get; set; }
    public string? TopicName { get; set; }
}

public class UpdateKafkaTopicProvisioningProgress : 
    IMessageHandler<KafkaTopicProvisioningHasBegun>,
    IMessageHandler<KafkaTopicProvisioningHasCompleted>
{
    private readonly ILogger<UpdateKafkaTopicProvisioningProgress> _logger;
    private readonly IKafkaTopicApplicationService _kafkaTopicApplicationService;
    private readonly IKafkaTopicRepository _kafkaTopicRepository;

    public UpdateKafkaTopicProvisioningProgress(ILogger<UpdateKafkaTopicProvisioningProgress> logger, IKafkaTopicApplicationService kafkaTopicApplicationService, 
        IKafkaTopicRepository kafkaTopicRepository)
    {
        _logger = logger;
        _kafkaTopicApplicationService = kafkaTopicApplicationService;
        _kafkaTopicRepository = kafkaTopicRepository;
    }

    private string ChangedBy => string.Join("/", "SYSTEM", GetType().FullName);

    private async Task<KafkaTopicId?> DetermineTopicIdFrom(string? kafkaTopicId, string? kafkaTopicName, string? kafkaClusterId)
    {
        if (KafkaTopicId.TryParse(kafkaTopicId, out var topicId))
        {
            return topicId;
        }

        if (!KafkaClusterId.TryParse(kafkaClusterId, out var clusterId))
        {
            return null;
        }

        if (!KafkaTopicName.TryParse(kafkaTopicName, out var topicName))
        {
            return null;
        }

        var topic = await _kafkaTopicRepository.FindBy(topicName, clusterId);
        return topic?.Id;
    }
    
    public async Task Handle(KafkaTopicProvisioningHasBegun message, MessageHandlerContext context)
    {
        using var _ = _logger.BeginScope("Handling {MessageType} on {ImplementationType} with {CorrelationId} and {CausationId}",
            context.MessageType, GetType().Name, context.CorrelationId, context.CausationId);

        var topicId = await DetermineTopicIdFrom(message.TopicId, message.TopicName, message.ClusterId);
        if (topicId is null)
        {
            _logger.LogError("Could not determine a valid kafka topic id using provided topic id {KafkaTopicId}, topic name {KafkaTopicName} or cluster id {KafkaClusterId} - skipping message {MessageId}/{MessageType}", 
                message.TopicId, message.TopicName, message.ClusterId, context.MessageId, context.MessageType);
            
            return;
        }

        await _kafkaTopicApplicationService.RegisterKafkaTopicAsInProgress(topicId, ChangedBy);
    }

    public async Task Handle(KafkaTopicProvisioningHasCompleted message, MessageHandlerContext context)
    {
        using var _ = _logger.BeginScope("Handling {MessageType} on {ImplementationType} with {CorrelationId} and {CausationId}",
            context.MessageType, GetType().Name, context.CorrelationId, context.CausationId);

        var topicId = await DetermineTopicIdFrom(message.TopicId, message.TopicName, message.ClusterId);
        if (topicId is null)
        {
            _logger.LogError("Could not determine a valid kafka topic id using provided topic id {KafkaTopicId}, topic name {KafkaTopicName} or cluster id {KafkaClusterId} - skipping message {MessageId}/{MessageType}", 
                message.TopicId, message.TopicName, message.ClusterId, context.MessageId, context.MessageType);
            
            return;
        }

        await _kafkaTopicApplicationService.RegisterKafkaTopicAsProvisioned(topicId, ChangedBy);
    }
}

public class CapabilityCreatedHandler : IMessageHandler<CapabilityCreated>
{
    private readonly IMembershipApplicationService _membershipApplicationService;

    public CapabilityCreatedHandler(IMembershipApplicationService membershipApplicationService)
    {
        _membershipApplicationService = membershipApplicationService;
    }

    public Task Handle(CapabilityCreated message, MessageHandlerContext context)
    {
        return _membershipApplicationService.AddCreatorAsInitialMember(message.CapabilityId, message.RequestedBy);
    }
}

public class AwsAccountRequestedHandler : IMessageHandler<AwsAccountRequested>
{
    private readonly IAwsAccountApplicationService _awsAccountApplicationService;

    public AwsAccountRequestedHandler(IAwsAccountApplicationService awsAccountApplicationService)
    {
        _awsAccountApplicationService = awsAccountApplicationService;
    }

    public async Task Handle(AwsAccountRequested message, MessageHandlerContext context)
    {
        if (!AwsAccountId.TryParse(message.AccountId, out var id))
        {
            throw new InvalidOperationException($"Invalid AwsAccountId {message.AccountId}");
        }

        await _awsAccountApplicationService.CreateAwsAccountRequestTicket(id);
    }
}
