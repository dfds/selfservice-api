using System;
using SelfService.Domain.Models;

namespace SelfService.Domain.Queries;

public interface IKafkaSchemaQuery
{
    Task<List<KafkaSchema>> ListSchemas();
}

public class KafkaSchemaQueryParams
{
    public string? SubjectPrefix { get; set; }
}
