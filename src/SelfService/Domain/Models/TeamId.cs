using Microsoft.AspNetCore.Http.HttpResults;

namespace SelfService.Domain.Models;

// Convert to generic when that gets merged in
public class TeamId : ValueObject
{
    public Guid Id { get; set; }

    private TeamId(Guid id)
    {
        Id = id;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        throw new NotImplementedException();
    }

    public static TeamId Create()
    {
        return new TeamId(Guid.NewGuid());
    }

    public static bool TryParse(Guid id, out TeamId o)
    {
        throw new NotImplementedException();
    }
}
