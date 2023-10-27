using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.Builders;

public class InvitationRepositoryBuilder
{
    private SelfServiceDbContext? _dbContext;

    public InvitationRepositoryBuilder WithDbContext(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
        return this;
    }

    public InvitationRepository Build()
    {
        return new InvitationRepository(_dbContext!);
    }

    public static implicit operator InvitationRepository(InvitationRepositoryBuilder builder) => builder.Build();
}
