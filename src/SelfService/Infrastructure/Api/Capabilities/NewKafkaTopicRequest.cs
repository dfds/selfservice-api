using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Capabilities;

public class NewKafkaTopicRequest
{
    [Required]
    public string? KafkaClusterId { get; set; }

    [Required]
    public string? Name { get; set; }

    [Required]
    public string? Description { get; set; }

    [Required]
    public uint? Partitions { get; set; }

    [Required]
    public string? Retention { get; set; }
}
