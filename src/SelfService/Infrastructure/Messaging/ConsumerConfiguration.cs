using Dafda.Configuration;
using Dafda.Consuming;
using SelfService.Domain;
using SelfService.Domain.Events;

namespace SelfService.Infrastructure.Messaging;

public static class ConsumerConfiguration
{
    private const string TopicPrefix = "cloudengineering.selfservice";

    public static void AddMessaging(this WebApplicationBuilder builder)
    {
        builder.Services.AddOutbox(options =>
        {
            options.WithOutboxEntryRepository<OutboxEntryRepository>();

            options
                .ForTopic($"{TopicPrefix}.kafkatopic")
                .Register<NewKafkaTopicHasBeenRequested>(
                    messageType: "new-kafka-topic-has-been-requested",
                    keySelector: x => x.KafkaTopicId!
                )
                ;

            options
                .ForTopic($"{TopicPrefix}.membershipapplication")
                .Register<NewMembershipApplicationHasBeenSubmitted>(
                    messageType: "new-membership-application-has-been-submitted",
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
                .ForTopic($"{TopicPrefix}.kafkatopic")
                .Ignore("new-kafka-topic-has-been-requested")
                ;

            options
                .ForTopic($"{TopicPrefix}.membershipapplication")
                .Ignore("new-membership-application-has-been-submitted")
                ;
        });
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