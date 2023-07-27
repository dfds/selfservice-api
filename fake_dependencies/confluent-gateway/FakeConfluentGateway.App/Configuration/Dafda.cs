using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dafda.Configuration;
using Dafda.Consuming;
using Dafda.Producing;
using Dafda.Serializing;

namespace FakeConfluentGateway.App.Configuration;

public static class Dafda
{
    private const string SelfServicePrefix = "cloudengineering.selfservice";
    private const string ConfluentGatewayPrefix = "cloudengineering.confluentgateway";
    private const string LegacyTopic = "build.selfservice.events.capabilities";

    public static void ConfigureDafda(this WebApplicationBuilder builder)
    {
        builder.Services.AddConsumer(options =>
        {
            options.WithConfigurationSource(builder.Configuration);
            options.WithEnvironmentStyle("DEFAULT_KAFKA");

            #region Fake Confluent Gateway

            options
                .ForTopic($"{SelfServicePrefix}.kafkatopic")
                .Ignore("topic-deleted")
                .Ignore("cluster-access-requested")
                .RegisterMessageHandler<NewKafkaTopicHasBeenRequestedMessage, NewKafkaTopicHasBeenRequestedHandler>(
                    "topic-requested")
                ;


            options
                .ForTopic($"{SelfServicePrefix}.messagecontract")
                .Ignore("message-contract-requested")
                .Ignore("message-contract-provisioned")
                ;

            options
                .ForTopic($"{ConfluentGatewayPrefix}.schema")
                .Ignore("schema-registered")
                .Ignore("schema-registration-failed")
                ;

            #endregion

            #region Fake aad-aws-sync

            options
                .ForTopic($"{SelfServicePrefix}.membership")
                .Ignore("user-has-joined-capability")
                ;

            #endregion
        });

        builder.Services.AddProducerFor<ConfluentGateway>(options =>
        {
            options.WithConfigurationSource(builder.Configuration);
            options.WithEnvironmentStyle("DEFAULT_KAFKA");
            options.Register<KafkaTopicProvisioningHasCompleted>($"{ConfluentGatewayPrefix}.provisioning",
                "topic-provisioned", (msg) => msg.TopicId);
        });

        builder.Services.AddProducerFor<LegacyProducer>(options =>
        {
            options.WithConfigurationSource(builder.Configuration);
            options.WithEnvironmentStyle("DEFAULT_KAFKA");
            options.WithDefaultPayloadSerializer(new LegacyContractPayloadSerializer());

            #region Fake org-account-context (pipeline)

            options.Register<AwsContextAccountCreated>(LegacyTopic,
                AwsContextAccountCreated.EventType, x => x.ContextId);

            #endregion

            #region Fake K8sJanitor

            options.Register<K8sNamespaceCreatedAndAwsArnConnected>(LegacyTopic,
                K8sNamespaceCreatedAndAwsArnConnected.EventType, x => x.ContextId);

            #endregion
        });
    }

    private class LegacyEventEnvelope
    {
        public string? Version { get; set; }
        public string? EventName { get; set; }

        [JsonPropertyName("x-correlationId")] public string? CorrelationId { get; set; }

        [JsonPropertyName("x-sender")] public string? Sender { get; set; }

        public object? Payload { get; set; }
    }

    private class LegacyContractPayloadSerializer : IPayloadSerializer
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public string PayloadFormat => "application/json";

        public Task<string> Serialize(PayloadDescriptor payloadDescriptor)
        {
            var message = new LegacyEventEnvelope()
            {
                Version = "1",
                EventName = payloadDescriptor.MessageType,
                CorrelationId = Guid.NewGuid().ToString("N"),
                Sender = Assembly.GetExecutingAssembly().FullName,
                Payload = payloadDescriptor.MessageData
            };
            var messageFrom = JsonSerializer.Serialize(message, JsonSerializerOptions);

            return Task.FromResult(messageFrom);
        }
    }
}

public class ConfluentGateway
{
    private readonly Producer _producer;

    public ConfluentGateway(Producer producer)
    {
        _producer = producer;
    }

    public async Task ProduceKafkaTopicProvisioningHasCompletedMessage(string topicId)
    {
        await _producer.Produce(new KafkaTopicProvisioningHasCompleted()
        {
            TopicId = topicId
        });
    }
}

public class LegacyProducer
{
    private readonly Producer _producer;

    public LegacyProducer(Producer producer)
    {
        _producer = producer;
    }

    public Task SendAwsContextAccountCreated(AwsContextAccountCreated message)
    {
        Console.WriteLine("Sending AwsContextAccountCreated message");
        return _producer.Produce(message);
    }

    public Task SendK8sNamespaceCreatedAndAwsArnConnected(K8sNamespaceCreatedAndAwsArnConnected message)
    {
        Console.WriteLine("Sending K8sNamespaceCreatedAndAwsArnConnected message");
        return _producer.Produce(message);
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

public class K8sNamespaceCreatedAndAwsArnConnected
{
    public const string EventType = "k8s_namespace_created_and_aws_arn_connected";

    public string? CapabilityId { get; set; }
    public string? ContextId { get; set; }
    public string? NamespaceName { get; set; }
}

public class KafkaTopicProvisioningHasCompleted
{
    public string? TopicId { get; set; }
}

public class NewKafkaTopicHasBeenRequestedMessage
{
    public string KafkaTopicId { get; set; }
}

public class NewKafkaTopicHasBeenRequestedHandler : IMessageHandler<NewKafkaTopicHasBeenRequestedMessage>
{
    private readonly ConfluentGateway _gateway;

    public NewKafkaTopicHasBeenRequestedHandler(ConfluentGateway gateway)
    {
        _gateway = gateway;
    }


    public async Task Handle(NewKafkaTopicHasBeenRequestedMessage message, MessageHandlerContext context)
    {
        await Task.Delay(1000);
        await _gateway.ProduceKafkaTopicProvisioningHasCompletedMessage(message.KafkaTopicId);
    }
}