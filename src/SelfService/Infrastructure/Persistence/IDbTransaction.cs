namespace SelfService.Infrastructure.Persistence;

public interface IDbTransaction : IDisposable, IAsyncDisposable
{
    Task Commit();
    Task Rollback();
}
