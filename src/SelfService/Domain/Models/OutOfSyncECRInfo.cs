namespace SelfService.Domain.Models;

public class OutOfSyncECRInfo : ValueObject
{
    public int RepositoriesNotInAwsCount;
    public int RepositoriesNotInDbCount;
    public List<string> RepositoriesNotInAws;
    public List<string> RepositoriesNotInDb;
    public bool HasBeenSet;
    public DateTime UpdateNeededAt;

    public OutOfSyncECRInfo(
        int repositoriesNotInAwsCount,
        int repositoriesNotInDbCount,
        List<string> repositoriesNotInAws,
        List<string> repositoriesNotInDb,
        bool hasBeenSet,
        DateTime updateNeededAt
    )
    {
        RepositoriesNotInAwsCount = repositoriesNotInAwsCount;
        RepositoriesNotInDbCount = repositoriesNotInDbCount;
        RepositoriesNotInAws = repositoriesNotInAws;
        RepositoriesNotInDb = repositoriesNotInDb;
        HasBeenSet = hasBeenSet;
        UpdateNeededAt = updateNeededAt;
    }

    public static OutOfSyncECRInfo createNewEmpty()
    {
        return new OutOfSyncECRInfo(0, 0, new List<string>(), new List<string>(), false, DateTime.Now.AddMinutes(719)); //11hrs59min
    }

    public void SetValuesFromInstance(OutOfSyncECRInfo o)
    {
        RepositoriesNotInAwsCount = o.RepositoriesNotInAwsCount;
        RepositoriesNotInDbCount = o.RepositoriesNotInDbCount;
        RepositoriesNotInAws = o.RepositoriesNotInAws;
        RepositoriesNotInDb = o.RepositoriesNotInDb;
        HasBeenSet = o.HasBeenSet;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        throw new NotImplementedException();
    }
}
