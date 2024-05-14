namespace SelfService.Infrastructure.Persistence;

public interface IAzureResourceManifestRepository
{
    public HashSet<String> GetAll();
}