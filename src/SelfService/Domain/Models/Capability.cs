namespace SelfService.Domain.Models
{
    public class Capability
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? Deleted { get; set; }
        public List<Membership> Memberships { get; set; } = new();
        public AwsAccount? AwsAccount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }
}