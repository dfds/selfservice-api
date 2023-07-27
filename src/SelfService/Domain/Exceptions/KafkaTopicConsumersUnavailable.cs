namespace SelfService.Domain.Exceptions;

public class KafkaTopicConsumersUnavailable : Exception
{
    public KafkaTopicConsumersUnavailable(string message) : base(message)
    {
        
    }
}
