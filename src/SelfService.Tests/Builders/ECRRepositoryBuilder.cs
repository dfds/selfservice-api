using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class ECRRepositoryBuilder
{
    private ECRRepositoryId _id;
    private string _name;
    private string _description;
    private string _createdBy;
    private DateTime? _requestedAt;

    public ECRRepositoryBuilder()
    {
        _id = Guid.NewGuid();
        _name = "ecr repo test";
        _description = "this is the description for ecr repo test";
        _createdBy = nameof(ECRRepositoryBuilder);
        _requestedAt = DateTime.UtcNow;
    }

    public ECRRepositoryBuilder WithId(ECRRepositoryId id)
    {
        _id = id;
        return this;
    }

    public ECRRepositoryBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ECRRepositoryBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public ECRRepositoryBuilder WithCreatedBy(string createdBy)
    {
        _createdBy = createdBy;
        return this;
    }

    public ECRRepository Build()
    {
        return new ECRRepository(id: _id, name: _name, description: _description, createdBy: _createdBy);
    }
}
