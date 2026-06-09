namespace SelfService.Infrastructure.Persistence;

public class RealDbTransactionFacade : IDbTransactionFacade
{
    private readonly SelfServiceDbContext _dbContext;

    public RealDbTransactionFacade(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IDbTransaction> BeginTransaction()
    {
        if (_dbContext.Database.CurrentTransaction != null)
        {
            return new NoOpDbTransaction(_dbContext);
        }

        var transaction = await _dbContext.Database.BeginTransactionAsync();

        return new RealDbTransaction(_dbContext, transaction);
    }
}
