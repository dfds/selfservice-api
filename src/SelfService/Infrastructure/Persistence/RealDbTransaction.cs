using Microsoft.EntityFrameworkCore.Storage;

namespace SelfService.Infrastructure.Persistence;

public class RealDbTransaction : IDbTransaction
{
    private readonly SelfServiceDbContext _dbContext;
    private readonly IDbContextTransaction _transaction;

    public RealDbTransaction(SelfServiceDbContext dbContext, IDbContextTransaction transaction)
    {
        _dbContext = dbContext;
        _transaction = transaction;
    }

    public async Task Commit()
    {
        await _dbContext.SaveChangesAsync();
        await _transaction.CommitAsync();
    }

    public async Task Rollback()
    {
        await _transaction.RollbackAsync();
    }

    public void Dispose()
    {
        _transaction.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _transaction.DisposeAsync();
    }
}