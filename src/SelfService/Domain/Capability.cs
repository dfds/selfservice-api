namespace SelfService.Domain
{
    public class Capability
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? RootId { get; set; }
        public DateTime? Deleted { get; set; }
        public List<Context> Contexts { get; } = new();
        public List<Membership> Memberships { get; set; } = new();
    }
}