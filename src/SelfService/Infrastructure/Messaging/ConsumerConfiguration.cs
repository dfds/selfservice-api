using Dafda.Configuration;
using SelfService.Domain;

namespace SelfService.Infrastructure.Messaging;

public static class ConsumerConfiguration
{
    public static void AddMessaging(this WebApplicationBuilder builder)
    {
        var topic = builder.Configuration["SS_APISPECS_TOPIC"];

        builder.Services.AddConsumer(options =>
        {
            options.WithConfigurationSource(builder.Configuration);
            options.WithEnvironmentStyle("DEFAULT_KAFKA");

            options.RegisterMessageHandler<Placeholder, PlaceholderHandler>(topic, Placeholder.EventType);
        });
    }
}