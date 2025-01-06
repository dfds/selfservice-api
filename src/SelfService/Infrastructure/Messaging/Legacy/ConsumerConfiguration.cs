using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Dafda.Configuration;
using Dafda.Consuming;

namespace SelfService.Infrastructure.Messaging.Legacy;

public static class ConsumerConfiguration
{
    public static void AddLegacyMessaging(this WebApplicationBuilder builder)
    {
        builder.Services.AddConsumer(options =>
        {
            options.WithConfigurationSource(builder.Configuration);
            options.WithEnvironmentStyle("DEFAULT_KAFKA");
            options.WithGroupId("selfservice.consumer.legacy");

            options.WithIncomingMessageFactory(_ => new LegacyIncomingMessageFactory());

            options
                .ForTopic("build.selfservice.events.capabilities")
                .RegisterMessageHandler<AwsContextAccountCreated, AwsContextAccountCreatedHandler>(
                    AwsContextAccountCreated.EventType
                )
                .RegisterMessageHandler<
                    K8sNamespaceCreatedAndAwsArnConnected,
                    K8sNamespaceCreatedAndAwsArnConnectedHandler
                >(K8sNamespaceCreatedAndAwsArnConnected.EventType);

            options.WithUnconfiguredMessageHandlingStrategy<UseNoOpHandler>();
        });
    }
}

public class LegacyIncomingMessageFactory : IIncomingMessageFactory
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public TransportLevelMessage Create(string rawMessage)
    {
        var jsonDocument = JsonDocument.Parse(rawMessage);
        var envelope = jsonDocument.Deserialize<LegacyEventEnvelope>(JsonSerializerOptions);
        if (envelope == null)
        {
            throw new InvalidOperationException($"Unable to deserialize {nameof(LegacyEventEnvelope)}");
        }

        var dict = new Dictionary<string, string?>
        {
            ["type"] = envelope.EventName,
            ["messageId"] = envelope.CorrelationId,
            ["correlationId"] = envelope.CorrelationId,
            ["causationId"] = envelope.CorrelationId,
            ["version"] = envelope.Version,
            ["sender"] = envelope.Sender,
        };

        var metadata = new Metadata(dict);

        return new TransportLevelMessage(
            metadata,
            type =>
            {
                using (jsonDocument)
                {
                    return envelope.Payload.Deserialize(type, JsonSerializerOptions);
                }
            }
        );
    }

    private class LegacyEventEnvelope
    {
        public string? Version { get; set; }
        public string? EventName { get; set; }

        [JsonPropertyName("x-correlationId")]
        public string? CorrelationId { get; set; }

        [JsonPropertyName("x-sender")]
        public string? Sender { get; set; }

        public JsonObject? Payload { get; set; }
    }
}
