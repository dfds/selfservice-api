namespace SelfService.Domain.Models;

public interface IDomainEvent
{
    // Marker interface
}

[AttributeUsage(AttributeTargets.Class)]
public class DomainEventDescriptorAttribute : Attribute
{
    public DomainEventDescriptorAttribute(string topic, string messageType)
    {
        Topic = topic;
        MessageType = messageType;
    }

    public string Topic { get; private set; }
    public string MessageType { get; private set; }
}

[AttributeUsage(AttributeTargets.Property)]
public class PartitionKey : Attribute { }
