using SelfService.Domain;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.Builders;

public class MembershipApplicationRepositoryBuilder
{
    private SelfServiceDbContext? _dbContext;
    private SystemTime _systemTime;

    public MembershipApplicationRepositoryBuilder()
    {
        _systemTime = SystemTime.Default;
    }

    public MembershipApplicationRepositoryBuilder WithDbContext(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
        return this;
    }

    public MembershipApplicationRepositoryBuilder WithSystemTime(SystemTime systemTime)
    {
        _systemTime = systemTime;
        return this;
    }

    public MembershipApplicationRepository Build()
    {
        return new MembershipApplicationRepository(_dbContext!, _systemTime);
    }

    public static implicit operator MembershipApplicationRepository(MembershipApplicationRepositoryBuilder builder)
        => builder.Build();
}