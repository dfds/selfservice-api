using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.Builders;

public class DemoRecordingRepositoryBuilder
{
    private SelfServiceDbContext? _dbContext;

    public DemoRecordingRepositoryBuilder WithDbContext(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
        return this;
    }

    public DemoRecordingRepository Build()
    {
        return new DemoRecordingRepository(_dbContext!);
    }

    public static implicit operator DemoRecordingRepository(DemoRecordingRepositoryBuilder builder) => builder.Build();
}
