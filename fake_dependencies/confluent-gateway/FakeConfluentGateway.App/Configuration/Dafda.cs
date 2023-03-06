using Dafda.Configuration;

namespace FakeConfluentGateway.App.Configuration;

public static class Dafda
{
    private const string TopicPrefix = "cloudengineering.selfservice";

    public static void ConfigureDafda(this WebApplicationBuilder builder)
    {
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