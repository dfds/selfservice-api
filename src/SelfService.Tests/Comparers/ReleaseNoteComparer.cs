using SelfService.Domain.Models;

namespace SelfService.Tests.Comparers;

public class ReleaseNoteComparer : IEqualityComparer<ReleaseNote?>
{
    public bool Equals(ReleaseNote? x, ReleaseNote? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (ReferenceEquals(x, null))
        {
            return false;
        }

        if (ReferenceEquals(y, null))
        {
            return false;
        }

        if (x.GetType() != y.GetType())
        {
            return false;
        }

        return x.Id == y.Id
            && x.Title == y.Title
            && x.Content == y.Content
            && x.ReleaseDate.Equals(y.ReleaseDate)
            && x.CreatedAt.Equals(y.CreatedAt)
            && x.ModifiedAt.Equals(y.ModifiedAt)
            && x.CreatedBy == y.CreatedBy
            && x.ModifiedBy == y.ModifiedBy
            && x.IsActive == y.IsActive;
    }

    public int GetHashCode(ReleaseNote obj)
    {
        return HashCode.Combine(
            obj.Id,
            obj.Title,
            obj.Content,
            obj.ReleaseDate,
            obj.IsActive
        );
    }
}
