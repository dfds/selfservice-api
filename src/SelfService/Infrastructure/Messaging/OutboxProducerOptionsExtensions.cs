using Dafda.Configuration;

namespace SelfService.Infrastructure.Messaging;

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