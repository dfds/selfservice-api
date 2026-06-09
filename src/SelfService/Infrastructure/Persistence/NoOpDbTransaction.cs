namespace SelfService.Infrastructure.Persistence;

/// <summary>
/// Returned by <see cref="RealDbTransactionFacade"/> when a transaction is already active on the
/// connection. Commit flushes EF tracking changes into the existing transaction buffer but does not
/// commit; the outer <see cref="RealDbTransaction"/> owns the actual commit/rollback.
/// </summary>
internal sealed class NoOpDbTransaction : IDbTransaction
{
    private readonly SelfServiceDbContext _dbContext;

    public NoOpDbTransaction(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Commit()
    {
        // Flush any pending EF changes into the outer transaction buffer.
        await _dbContext.SaveChangesAsync();
    }

    public Task Rollback() => Task.CompletedTask;

    public void Dispose() { }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
