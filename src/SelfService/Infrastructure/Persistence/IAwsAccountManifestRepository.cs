namespace SelfService.Infrastructure.Persistence;

public interface IAwsAccountManifestRepository
{
    public HashSet<String> GetAll();
    public Task Add(AwsAccountManifest manifest);
}
