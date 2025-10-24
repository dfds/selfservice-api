using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.Builders;

public class DemosRepositoryBuilder
{
    private SelfServiceDbContext? _dbContext;

    public DemosRepositoryBuilder WithDbContext(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
        return this;
    }

    public DemosRepository Build()
    {
        return new DemosRepository(_dbContext!);
    }

    public static implicit operator DemosRepository(DemosRepositoryBuilder builder) => builder.Build();
}
