using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.Builders;

public class CapabilityRepositoryBuilder
{
    private SelfServiceDbContext? _dbContext;

    public CapabilityRepositoryBuilder WithDbContext(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
        return this;
    }

    public CapabilityRepository Build()
    {
        return new CapabilityRepository(_dbContext!);
    }

    public static implicit operator CapabilityRepository(CapabilityRepositoryBuilder builder) => builder.Build();
}
