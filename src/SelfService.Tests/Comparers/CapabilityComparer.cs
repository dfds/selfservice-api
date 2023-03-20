using SelfService.Domain.Models;

namespace SelfService.Tests.Comparers;

public class CapabilityComparer : IEqualityComparer<Capability?>
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

public class MemberComparer : IEqualityComparer<Member?>
{
    public bool Equals(Member? x, Member? y)
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

        return x.Email == y.Email && x.DisplayName == y.DisplayName;
    }

    public int GetHashCode(Member obj)
    {
        return HashCode.Combine(obj.Email, obj.DisplayName);
    }
}

public class MembershipComparer : IEqualityComparer<Membership?>
{
    public bool Equals(Membership? x, Membership? y)
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

        return x.CapabilityId.Equals(y.CapabilityId) && 
               x.UserId.Equals(y.UserId) && 
               x.CreatedAt.Equals(y.CreatedAt);
    }

    public int GetHashCode(Membership obj)
    {
        return HashCode.Combine(obj.CapabilityId, obj.UserId, obj.CreatedAt);
    }
}

public class AwsAccountComparer : IEqualityComparer<AwsAccount?>
{
    public bool Equals(AwsAccount? x, AwsAccount? y)
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

        return x.CapabilityId.Equals(y.CapabilityId) &&
               x.AccountId.Equals(y.AccountId) &&
               x.RoleArn.Equals(y.RoleArn) &&
               x.RoleEmail == y.RoleEmail &&
               x.CreatedAt.Equals(y.CreatedAt) &&
               x.CreatedBy == y.CreatedBy;
    }

    public int GetHashCode(AwsAccount obj)
    {
        return HashCode.Combine(obj.CapabilityId, obj.AccountId, obj.RoleArn, obj.RoleEmail, obj.CreatedAt, obj.CreatedBy);
    }
}

public class KafkaClusterComparer : IEqualityComparer<KafkaCluster?>
{
    public bool Equals(KafkaCluster? x, KafkaCluster? y)
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

        return x.Name == y.Name && x.Description == y.Description && x.Enabled == y.Enabled;
    }

    public int GetHashCode(KafkaCluster obj)
    {
        return HashCode.Combine(obj.Name, obj.Description, obj.Enabled);
    }
}

public class KafkaTopicComparer : IEqualityComparer<KafkaTopic?>
{
    public bool Equals(KafkaTopic? x, KafkaTopic? y)
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

        return x.CapabilityId.Equals(y.CapabilityId) &&
               x.KafkaClusterId.Equals(y.KafkaClusterId) &&
               x.Name.Equals(y.Name) &&
               x.Description == y.Description &&
               x.Status == y.Status &&
               x.Partitions == y.Partitions &&
               x.Retention == y.Retention &&
               x.CreatedAt.Equals(y.CreatedAt) &&
               x.CreatedBy == y.CreatedBy &&
               Nullable.Equals(x.ModifiedAt, y.ModifiedAt) &&
               x.ModifiedBy == y.ModifiedBy;
    }

    public int GetHashCode(KafkaTopic obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.CapabilityId);
        hashCode.Add(obj.KafkaClusterId);
        hashCode.Add(obj.Name);
        hashCode.Add(obj.Description);
        hashCode.Add((int) obj.Status);
        hashCode.Add(obj.Partitions);
        hashCode.Add(obj.Retention);
        hashCode.Add(obj.CreatedAt);
        hashCode.Add(obj.CreatedBy);
        hashCode.Add(obj.ModifiedAt);
        hashCode.Add(obj.ModifiedBy);
        return hashCode.ToHashCode();
    }
}

public class MembershipApplicationComparer : IEqualityComparer<MembershipApplication?>
{
    public bool Equals(MembershipApplication? x, MembershipApplication? y)
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

        return x.CapabilityId.Equals(y.CapabilityId) &&
               x.Applicant.Equals(y.Applicant) &&
               x.Status.Equals(y.Status) &&
               x.SubmittedAt.Equals(y.SubmittedAt) &&
               x.ExpiresOn.Equals(y.ExpiresOn) &&
               x.Approvals.SequenceEqual(y.Approvals);
    }

    public int GetHashCode(MembershipApplication obj)
    {
        return HashCode.Combine(
            obj.CapabilityId,
            obj.Applicant,
            obj.Status,
            obj.SubmittedAt,
            obj.ExpiresOn,
            obj.Applicant
        );
    }
}

public class MembershipApprovalComparer : IEqualityComparer<MembershipApproval?>
{
    public bool Equals(MembershipApproval? x, MembershipApproval? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.ApprovedBy.Equals(y.ApprovedBy) && x.ApprovedAt.Equals(y.ApprovedAt);
    }

    public int GetHashCode(MembershipApproval obj)
    {
        return HashCode.Combine(obj.ApprovedBy, obj.ApprovedAt);
    }
}

public class MessageContractComparer : IEqualityComparer<MessageContract?>
{
    public bool Equals(MessageContract? x, MessageContract? y)
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

        return x.KafkaTopicId.Equals(y.KafkaTopicId) &&
               x.MessageType.Equals(y.MessageType) &&
               x.Example.Equals(y.Example) &&
               x.Schema.Equals(y.Schema) &&
               x.Description == y.Description &&
               x.Status.Equals(y.Status) &&
               x.CreatedAt.Equals(y.CreatedAt) &&
               x.CreatedBy == y.CreatedBy &&
               Nullable.Equals(x.ModifiedAt, y.ModifiedAt) &&
               x.ModifiedBy == y.ModifiedBy;
    }

    public int GetHashCode(MessageContract obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.KafkaTopicId);
        hashCode.Add(obj.MessageType);
        hashCode.Add(obj.Example);
        hashCode.Add(obj.Schema);
        hashCode.Add(obj.Description);
        hashCode.Add(obj.Status);
        hashCode.Add(obj.CreatedAt);
        hashCode.Add(obj.CreatedBy);
        hashCode.Add(obj.ModifiedAt);
        hashCode.Add(obj.ModifiedBy);
        return hashCode.ToHashCode();
    }
}
