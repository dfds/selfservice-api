namespace SelfService.Domain.Models;

public class OutOfSyncECRInfo : ValueObject
{
    public int RepositoriesNotInAwsCount;
    public int RepositoriesNotInDbCount;
    public List<string> RepositoriesNotInAws;
    public List<string> RepositoriesNotInDb;
    
    public OutOfSyncECRInfo(int repositoriesNotInAwsCount, int repositoriesNotInDbCount, List<string> repositoriesNotInAws, List<string>  repositoriesNotInDb)
    {
        RepositoriesNotInAwsCount = repositoriesNotInAwsCount;
        RepositoriesNotInDbCount = repositoriesNotInDbCount;
        RepositoriesNotInAws = repositoriesNotInAws;
        RepositoriesNotInDb = repositoriesNotInDb;
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