namespace SelfService.Domain.Models;

public class Team : Entity<TeamId>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Team(TeamId id, string name, string description, string createdBy, DateTime createdAt)
        : base(id)
    {
        Name = name;
        Description = description;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }
}
