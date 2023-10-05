namespace SelfService.Domain.Models;

public class ECRRepository : Entity<ECRRepositoryId>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string RepositoryName { get; private set; }
    public string CreatedBy { get; private set; }

    public DateTime? RequestedAt { get; private set; }

    public ECRRepository(
        ECRRepositoryId id,
        string name,
        string description,
        string repositoryName,
        string createdBy,
        DateTime? requestedAt
    )
        : base(id)
    {
        Name = name;
        Description = description;
        RepositoryName = repositoryName;
        CreatedBy = createdBy;
        RequestedAt = requestedAt;
    }
}
