using Dafda.Configuration;
using SelfService.Domain;
using SelfService.Domain.Events;

namespace SelfService.Infrastructure.Messaging;

public static class ConsumerConfiguration
{
    private const string TopicPrefix = "cloudengineering.selfservice";

    public static void AddMessaging(this WebApplicationBuilder builder)
    {
        var topic = builder.Configuration["SS_APISPECS_TOPIC"];

        builder.Services.AddConsumer(options =>
        {
            options.WithConfigurationSource(builder.Configuration);
            options.WithEnvironmentStyle("DEFAULT_KAFKA");

            options.RegisterMessageHandler<Placeholder, PlaceholderHandler>(topic, Placeholder.EventType);

            //options.RegisterMessageHandler<>();
        });

        builder.Services.AddOutbox(options =>
        {
            options.WithOutboxEntryRepository<OutboxEntryRepository>();
            

            options.Register<NewKafkaTopicHasBeenRequested>(
                topic: $"{TopicPrefix}.kafkatopic",
                type: "new-kafka-topic-has-been-requested",
                keySelector: x => x.KafkaTopicId
            );
        });
    }
}