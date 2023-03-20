namespace SelfService.Domain.Models;

[Obsolete("Turn this into a value object instead (see MessageContractStatus)")]
public enum KafkaTopicStatusType
{
    Unknown = 0,
    Requested = 1,
    InProgress = 2,
    Provisioned = 3,
}