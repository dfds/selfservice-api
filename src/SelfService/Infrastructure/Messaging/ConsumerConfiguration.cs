using Dafda.Configuration;
using Dafda.Consuming;
using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Events;
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
                .ForTopic($"{SelfServicePrefix}.membership")
                .Register<UserHasJoinedCapability>(
                    messageType: "user-has-joined-capability",
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
                    messageType: "membership-submitted",
                    keySelector: x => x.MembershipApplicationId!
                )
                ;
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
                .ForTopic($"{SelfServicePrefix}.kafkatopic")
                .Ignore("topic-requested")
                ;

            options
                .ForTopic($"{SelfServicePrefix}.membershipapplication")
                .Ignore("membership-submitted")
                ;

            options
                .ForTopic($"{ConfluentGatewayPrefix}.schema")
                .RegisterMessageHandler<SchemaRegistered, MarkMessageContractAsProvisioned>("schema-registered")
                ;
        });
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

public static class ConsumerOptionsExtensions
{
    public static TopicConsumerOptions ForTopic(this ConsumerOptions options, string topic)
    {
        return new TopicConsumerOptions(options, topic);
    }

    public class TopicConsumerOptions
    {
        private readonly ConsumerOptions _options;
        private readonly string _topic;

        public TopicConsumerOptions(ConsumerOptions options, string topic)
        {
            _options = options;
            _topic = topic;
        }

        public TopicConsumerOptions RegisterMessageHandler<TMessage, TMessageHandler>(
            string messageType)
            where TMessage : class
            where TMessageHandler : class, IMessageHandler<TMessage>
        {
            _options.RegisterMessageHandler<TMessage, TMessageHandler>(_topic, messageType);
            return this;
        }

        public TopicConsumerOptions Ignore(string messageType)
        {
            _options.RegisterMessageHandler<object, NoOpHandler>(_topic, messageType);
            return this;
        }
    }
}

public static class OutboxProducerOptionsExtensions
{
    public static ProducerOptions ForTopic(this OutboxOptions options, string topic)
    {
        return new ProducerOptions(options, topic);
    }

    public class ProducerOptions
    {
        private readonly OutboxOptions _options;
        private readonly string _topic;

        public ProducerOptions(OutboxOptions options, string topic)
        {
            _options = options;
            _topic = topic;
        }

        public ProducerOptions Register<TMessage>(string messageType, Func<TMessage, string> keySelector) where TMessage : class
        {
            _options.Register<TMessage>(
                topic: _topic,
                type: messageType,
                keySelector: keySelector
            );

            return this;
        }
    }
}