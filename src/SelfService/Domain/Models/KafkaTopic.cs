namespace SelfService.Domain.Models
{
    public class KafkaTopic
    {
        public Guid Id { get; set; }
        public string CapabilityId { get; set; }
        public Guid KafkaClusterId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public int Partitions { get; set; }
        public long Retention { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}