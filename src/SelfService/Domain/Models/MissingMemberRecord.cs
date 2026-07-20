namespace SelfService.Domain.Models;

public enum MissingMemberStatus
{
    NotFound,
    Deactivated,
}

public class MissingMemberRecord
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public MissingMemberStatus Status { get; set; }
    public DateTime FirstSeenMissingAt { get; set; }
    public DateTime LastCheckedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    private MissingMemberRecord()
    {
        UserId = string.Empty;
    }

    public MissingMemberRecord(string userId, MissingMemberStatus status, DateTime firstSeenAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Status = status;
        FirstSeenMissingAt = firstSeenAt;
        LastCheckedAt = firstSeenAt;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasGracePeriodExpired(int gracePeriodDays = 7)
    {
        return FirstSeenMissingAt.AddDays(gracePeriodDays) <= DateTime.UtcNow;
    }

    public void UpdateLastChecked()
    {
        LastCheckedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
