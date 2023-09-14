using System.ComponentModel.DataAnnotations.Schema;

namespace SelfService.Domain.Models;

public class SelfServiceJsonSchema
{
    public Guid Id { get; set; }
    public int SchemaVersion { get; set; }
    public string ObjectId { get; set; }

    [Column(TypeName = "jsonb")]
    public string Schema { get; set; }

    public SelfServiceJsonSchema(int schemaVersion, string objectId, string schema)
    {
        Id = Guid.NewGuid();
        ObjectId = objectId;
        SchemaVersion = schemaVersion;
        Schema = schema;
    }
}
