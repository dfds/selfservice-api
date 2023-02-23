using SelfService.Domain.Models;

namespace SelfService.Tests.Comparers;

public class CapabilityComparer : IEqualityComparer<Capability>
{
    public bool Equals(Capability? x, Capability? y)
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

        return x.Name == y.Name &&
               x.Description == y.Description &&
               Nullable.Equals(x.Deleted, y.Deleted) &&
               x.CreatedAt.Equals(y.CreatedAt) &&
               x.CreatedBy == y.CreatedBy;
    }

    public int GetHashCode(Capability obj)
    {
        return HashCode.Combine(obj.Name, obj.Description, obj.Deleted, obj.CreatedAt, obj.CreatedBy);
    }
}