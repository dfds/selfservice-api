using Dafda.Configuration;
using Dafda.Consuming;

namespace SelfService.Infrastructure.Messaging;

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