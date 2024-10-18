using System;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Kafka;

namespace SelfService.Application;

public class KafkaSchemaService : IKafkaSchemaService
{
    private readonly IConfluentGatewayService _confluentGatewayClient;

    public KafkaSchemaService(IConfluentGatewayService confluentGatewayClient)
    {
        _confluentGatewayClient = confluentGatewayClient;
    }

    public async Task<List<KafkaSchema>> ListSchemas(KafkaSchemaQueryParams queryParams)
    {
        try
        {
            return await _confluentGatewayClient.ListSchemas(queryParams);
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to list schemas", ex);
        }
    }
}
