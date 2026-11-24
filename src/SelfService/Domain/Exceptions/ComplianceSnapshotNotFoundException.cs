namespace SelfService.Domain.Exceptions;

public class ComplianceSnapshotNotFoundException : Exception
{
    public ComplianceSnapshotNotFoundException(DateOnly date)
        : base($"No compliance snapshot exists for {date:yyyy-MM-dd}.")
    {
        Date = date;
    }

    public DateOnly Date { get; }
}
