using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.Builders;

public class ECRRepositoryRepositoryBuilder
{
    private SelfServiceDbContext? _dbContext;

    public ECRRepositoryRepositoryBuilder WithDbContext(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
        return this;
    }

    public ECRRepositoryRepository Build()
    {
        return new ECRRepositoryRepository(_dbContext!);
    }

    public static implicit operator ECRRepositoryRepository(ECRRepositoryRepositoryBuilder builder) => builder.Build();
}
