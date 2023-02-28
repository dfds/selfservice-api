using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class KafkaTopicRepository : IKafkaTopicRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public KafkaTopicRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(KafkaTopic topic)
    {
        await _dbContext.KafkaTopics.AddAsync(topic);
    }

    public async Task<bool> Exists(KafkaTopicName name)
    {
        var found = await _dbContext.KafkaTopics
            .Where(x => x.Name == name)
            .FirstOrDefaultAsync();

        return found != null;
    }

    public async Task<KafkaTopic> Get(KafkaTopicId id)
    {
        var result = await _dbContext.KafkaTopics.FindAsync(id);
        if (result is null)
        {
            throw EntityNotFoundException<KafkaTopic>.UsingId(id);
        }

        return result;
    }
}