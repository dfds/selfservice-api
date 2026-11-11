using System.Linq.Expressions;

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

    /// <summary>
    /// Loads the whole table and filters it in memory.
    /// </summary>
    Task<List<T>> GetAllWithPredicate(Func<T, bool> predicate);

    /// <summary>
    /// Filters in the database. The predicate must be translatable to SQL by EF Core; an untranslatable
    /// one throws <see cref="InvalidOperationException"/> when the query runs.
    /// </summary>
    Task<List<T>> GetAllWhere(Expression<Func<T, bool>> predicate);
}
