using SelfService.Configuration;

namespace SelfService.Infrastructure.Persistence;

public interface IDbTransactionFacade
{
    Task<IDbTransaction> BeginTransaction();
}