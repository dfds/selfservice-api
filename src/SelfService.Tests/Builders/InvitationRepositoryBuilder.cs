using SelfService.Domain;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.Builders;

public class InvitationRepositoryBuilder
{
    private SelfServiceDbContext? _dbContext;

    private SystemTime _systemTime;

    public InvitationRepositoryBuilder()
    {
        _systemTime = SystemTime.Default;
    }

    public InvitationRepositoryBuilder WithDbContext(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
        return this;
    }

    public InvitationRepositoryBuilder WithSystemTime(SystemTime systemTime)
    {
        _systemTime = systemTime;
        return this;
    }

    public InvitationRepository Build()
    {
        return new InvitationRepository(_dbContext!, _systemTime);
    }

    public static implicit operator InvitationRepository(InvitationRepositoryBuilder builder) => builder.Build();
}
