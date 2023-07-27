using System.Linq.Expressions;

namespace SelfService.Domain.Exceptions;

public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string message) : base(message)
    {
        
    }
}

public class EntityNotFoundException<TEntity> : EntityNotFoundException
{
    public EntityNotFoundException(string message) : base(message)
    {
        
    }

    public static EntityNotFoundException<TEntity> UsingId(string? id)
    {
        return new EntityNotFoundException<TEntity>($"{typeof(TEntity).Name} with id \"{id}\" does not exist.");
    }
}

public class EntityAlreadyExistsException : Exception
{
    public EntityAlreadyExistsException(string message) : base(message)
    {
        
    }
}

public class EntityAlreadyExistsException<TEntity> : EntityAlreadyExistsException
{
    private EntityAlreadyExistsException(string message) : base(message)
    {
        
    }

    public static EntityAlreadyExistsException<TEntity> WithProperty(string propertyName, string? value)
    {
        return new EntityAlreadyExistsException<TEntity>($"{typeof(TEntity).Name} with \"{propertyName}\" set to \"{value}\" already exists.");
    }

    public static EntityAlreadyExistsException<TEntity> WithProperty(Expression<Func<TEntity, object>> property, string? value)
    {
        var expression = property.Body as MemberExpression;
        var propertyName = expression?.Member?.Name ?? "";

        return WithProperty(propertyName, value);
    }
}