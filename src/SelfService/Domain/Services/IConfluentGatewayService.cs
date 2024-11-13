using System;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Domain.Services;

public interface IConfluentGatewayService
{
    public Task<List<KafkaSchema>> ListSchemas(string clusterId, KafkaSchemaQueryParams queryParams);
}
