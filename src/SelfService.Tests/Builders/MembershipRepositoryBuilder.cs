using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.Builders;

public class MembershipRepositoryBuilder
{
    private SelfServiceDbContext? _dbContext;

    public MembershipRepositoryBuilder WithDbContext(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
        return this;
    }

    public MembershipRepository Build()
    {
        return new MembershipRepository(_dbContext!);
    }

    public static implicit operator MembershipRepository(MembershipRepositoryBuilder builder)
        => builder.Build();
}