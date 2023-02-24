namespace SelfService.Domain.Exceptions;

public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string message) : base(message)
    {
        
    }
}

public class EntityNotFoundException<TEntity> : Exception
{
    private EntityNotFoundException(string message) : base(message)
    {
        
    }

    public static EntityNotFoundException<TEntity> UsingId(string? id)
    {
        return new EntityNotFoundException<TEntity>($"{typeof(TEntity).Name} with id \"{id}\" does not exist.");
    }
}