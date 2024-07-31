using SelfService.Domain.Aspectly;

namespace SelfService.Infrastructure.Persistence;

public class TransactionalAspect : IAspect
{
    private readonly ILogger<TransactionalAspect> _logger;
    private readonly IDbTransactionFacade _transactionFacade;

    public TransactionalAspect(ILogger<TransactionalAspect> logger, IDbTransactionFacade transactionFacade)
    {
        _logger = logger;
        _transactionFacade = transactionFacade;
    }

    public async Task Invoke(AspectContext context, AspectDelegate next)
    {
        await using var transaction = await _transactionFacade.BeginTransaction();
        try
        {
            await next();
            await transaction.Commit();

            _logger.LogTrace(
                "Committed db transaction for {Method} on {Type}",
                context.Method.Name,
                context.Method.DeclaringType?.Name
            );
        }
        catch (Exception)
        {
            await transaction.Rollback();
            throw;
        }
    }
}
