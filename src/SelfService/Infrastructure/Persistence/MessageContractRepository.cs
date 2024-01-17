using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class MessageContractRepository : IMessageContractRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public MessageContractRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(MessageContract messageContract)
    {
        await _dbContext.MessageContracts.AddAsync(messageContract);
    }

    public void Update(MessageContract messageContract)
    {
        _dbContext.MessageContracts.Update(messageContract);
    }

    public async Task<MessageContract> Get(MessageContractId id)
    {
        var result = await _dbContext.MessageContracts.FindAsync(id);
        if (result is null)
        {
            throw EntityNotFoundException<MessageContract>.UsingId(id);
        }

        return result;
    }

    public async Task<MessageContract?> FindBy(MessageContractId id)
    {
        return await _dbContext.MessageContracts.FindAsync(id);
    }

    public async Task<IEnumerable<MessageContract>> FindBy(KafkaTopicId topicId)
    {
        return await _dbContext.MessageContracts
            .Where(x => x.KafkaTopicId == topicId)
            .OrderBy(x => x.MessageType)
            .ToListAsync();
    }

    public async Task<MessageContract?> GetLatestSchema(KafkaTopicId topicId, MessageType messageType)
    {
        var messageContracts = await _dbContext.MessageContracts
            .Where(x => x.KafkaTopicId == topicId)
            .Where(x => x.MessageType == messageType)
            .OrderBy(x => x.SchemaVersion)
            .ToListAsync();

        if (messageContracts.Count == 0)
            return null;

        return messageContracts.MaxBy(x => x.SchemaVersion);
    }

    public async Task<bool> Exists(KafkaTopicId topicId, MessageType messageType)
    {
        return await _dbContext.MessageContracts
            .Where(x => x.KafkaTopicId == topicId && x.MessageType == messageType)
            .AnyAsync();
    }

    public Task Delete(MessageContract messageContract)
    {
        _dbContext.MessageContracts.Remove(messageContract);
        return Task.CompletedTask;
    }
}
