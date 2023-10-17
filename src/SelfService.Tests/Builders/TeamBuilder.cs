using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class TeamBuilder
{
    private TeamId _id;
    private string _name;
    private string _description;
    private string _createdBy;
    private DateTime _requestedAt;

    public TeamBuilder()
    {
        _id = TeamId.New();
        _name = "team test";
        _description = "this is the description for team test";
        _createdBy = nameof(TeamBuilder);
        _requestedAt = DateTime.UtcNow;
    }

    public TeamBuilder WithId(TeamId id)
    {
        _id = id;
        return this;
    }

    public TeamBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TeamBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public TeamBuilder WithCreatedBy(string createdBy)
    {
        _createdBy = createdBy;
        return this;
    }

    public Team Build()
    {
        return new Team(
            id: _id,
            name: _name,
            description: _description,
            createdBy: _createdBy,
            createdAt: _requestedAt
        );
    }
}
