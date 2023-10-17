using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class GenericRepository<T, TId> : IGenericRepository<T, TId>
    where T : Entity<TId>
{
    protected readonly DbSet<T> DbSetReference;

    public GenericRepository(DbSet<T> dbSetReference)
    {
        DbSetReference = dbSetReference;
    }

    public async Task Add(T model)
    {
        await DbSetReference.AddAsync(model);
    }

    public async Task<bool> Exists(TId id)
    {
        var found = await DbSetReference.FindAsync(id);
        return found != null;
    }

    public async Task<T?> FindById(TId id)
    {
        return await DbSetReference.FindAsync(id);
    }

    public async Task<T?> FindByPredicate(Func<T, bool> predicate)
    {
        // Done in two steps to avoid Entity Framework Core from choking on the predicate.
        var ts = await DbSetReference.ToListAsync();
        return ts.FirstOrDefault(predicate);
    }

    public async Task<T> Remove(TId id)
    {
        var objectT = await FindById(id);
        if (objectT is null)
        {
            throw EntityNotFoundException<TId>.UsingId(id?.ToString());
        }

        DbSetReference.Remove(objectT);

        return objectT;
    }

    public Task<List<T>> GetAll()
    {
        return DbSetReference.ToListAsync();
    }

    public async Task<List<T>> GetAllWithPredicate(Func<T, bool> predicate)
    {
        // Done in two steps to avoid Entity Framework Core from choking on the predicate.
        var ts = await DbSetReference.ToListAsync();
        return ts.Where(predicate).ToList();
    }
}
