namespace SelfService.Domain.Models;

public class ECRRepository : Entity<ECRRepositoryId>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string CreatedBy { get; private set; }

    public ECRRepository(ECRRepositoryId id, string name, string description, string createdBy)
        : base(id)
    {
        Name = name;
        Description = description;
        CreatedBy = createdBy;
    }
}
