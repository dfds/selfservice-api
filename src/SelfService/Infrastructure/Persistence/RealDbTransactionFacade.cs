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
        var transaction = await _dbContext.Database.BeginTransactionAsync();

        return new RealDbTransaction(_dbContext, transaction);
    }
}