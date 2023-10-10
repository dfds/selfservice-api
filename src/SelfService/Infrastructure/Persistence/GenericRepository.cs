using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class GenericRepository<T, TId> : IGenericRepository<T, TId>
    where T : Entity<TId>
{
    private readonly DbSet<T> _dbSetReference;

    public GenericRepository(DbSet<T> dbSetReference)
    {
        _dbSetReference = dbSetReference;
    }

    public async Task Add(T model)
    {
        await _dbSetReference.AddAsync(model);
    }

    public async Task<bool> Exists(TId id)
    {
        var found = await _dbSetReference.FindAsync(id);
        return found != null;
    }

    public async Task<T?> FindBy(TId id)
    {
        return await _dbSetReference.FindAsync(id);
    }

    public async Task<T> Remove(TId id)
    {
        var objectT = await FindBy(id);
        if (objectT is null)
        {
            throw EntityNotFoundException<TId>.UsingId(id?.ToString());
        }

        _dbSetReference.Remove(objectT);

        return objectT;
    }

    public Task<List<T>> GetAll()
    {
        return _dbSetReference.ToListAsync();
    }
}
