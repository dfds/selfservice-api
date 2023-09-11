namespace SelfService.Domain.Models;

public class ECRRepository : Entity<ECRRepositoryId>
{
    public ECRRepositoryId EcrRepositoryId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string RepositoryName { get; private set; }
    public string CreatedBy { get; private set; }

    public ECRRepository(ECRRepositoryId id, string name, string description, string repositoryName, string createdBy)
        : base(id)
    {
        EcrRepositoryId = id;
        Name = name;
        Description = description;
        RepositoryName = repositoryName;
        CreatedBy = createdBy;
    }
}
