namespace SelfService.Domain.Models;

public class Capability : AggregateRoot<CapabilityId>
{
    protected Capability() { }

    public Capability(CapabilityId id, string name, string description, DateTime? deleted, DateTime createdAt, string createdBy) : base(id)
    {
        Name = name;
        Description = description;
        Deleted = deleted;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public string Name { get; private set; }
    public string Description { get; set; }
    public DateTime? Deleted { get; set; }

    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; }

    public override string ToString()
    {
        return Id.ToString();
    }
}