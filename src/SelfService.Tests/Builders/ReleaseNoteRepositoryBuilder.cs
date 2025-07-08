using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.Builders;

public class ReleaseNoteRepositoryBuilder
{
    private SelfServiceDbContext? _dbContext;

    public ReleaseNoteRepositoryBuilder WithDbContext(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
        return this;
    }

    public ReleaseNoteRepository Build()
    {
        return new ReleaseNoteRepository(_dbContext!);
    }

    public static implicit operator ReleaseNoteRepository(ReleaseNoteRepositoryBuilder builder) => builder.Build();
}
