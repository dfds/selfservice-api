using System;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Application;

public interface IKafkaSchemaService
{
    public Task<List<KafkaSchema>> ListSchemas(KafkaSchemaQueryParams queryParams);

}
