namespace SelfService.Domain.Models;

public interface IGenericRepository<T, TId>
    where T : Entity<TId>
{
    Task Add(T model);
    Task<bool> Exists(TId id);
    Task<T?> FindById(TId id);
    Task<T?> FindByPredicate(Func<T, bool> predicate);
    Task<T> Remove(TId id);
    Task<List<T>> GetAll();
}
