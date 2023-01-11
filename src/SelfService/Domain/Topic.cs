namespace SelfService.Domain
{
    public class Topic
    {
        public Guid Id { get; set; }
        public Guid CapabilityId { get; set; }
        public Guid KafkaClusterId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public int Partitions { get; set; }
        public Dictionary<string, object> Configurations { get; set; } = new();
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }

    }
}