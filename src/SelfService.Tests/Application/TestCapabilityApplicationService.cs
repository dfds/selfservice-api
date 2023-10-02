using Moq;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Tests.Application;

public class TestCapabilityApplicationService
{
    private const string TestJsonMetadata = """
                                                {
                                                    "foo": "bar"
                                                }
                                            """;

    private const string InvalidJsonMetadata = """
                                                   {
                                                       "not": "valid"
                                                   }
                                               """;

    private const string SchemaWithRequiredField = """
                                                   {
                                                     "$schema": "https://json-schema.org/draft/2020-12/schema",
                                                     "required":["foo"],
                                                     "additionalProperties": false,
                                                     "properties":
                                                       {
                                                         "foo": {
                                                           "type": "string"
                                                         }
                                                       }
                                                   }
                                                   """;

    private readonly Mock<ISelfServiceJsonSchemaRepository> _mock = new();

    [Fact]
    public async Task can_set_json_metadata()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();
        var repo = A.CapabilityRepository.WithDbContext(dbContext).Build();

        var newCapability = A.Capability.Build();
        await repo.Add(newCapability);
        await dbContext.SaveChangesAsync();

        var capabilityService = A.CapabilityApplicationService.WithCapabilityRepository(repo).Build();

        await capabilityService.SetJsonMetadata(newCapability.Id, TestJsonMetadata);

        var capabilityWithJsonMetadata = await repo.Get(newCapability.Id);
        Assert.Equal(TestJsonMetadata, capabilityWithJsonMetadata.JsonMetadata);
        Assert.Equal(0, capabilityWithJsonMetadata.JsonMetadataSchemaVersion);
    }

    [Fact]
    public async Task can_set_json_metadata_with_schema_check()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();
        var capabilityRepository = A.CapabilityRepository.WithDbContext(dbContext).Build();

        var newCapability = A.Capability.Build();
        await capabilityRepository.Add(newCapability);
        await dbContext.SaveChangesAsync();

        _mock
            .Setup(x => x.GetLatestSchema(SelfServiceJsonSchemaObjectId.Capability))
            .ReturnsAsync(
                () => new SelfServiceJsonSchema(1, SelfServiceJsonSchemaObjectId.Capability, SchemaWithRequiredField)
            );
        var sut = A.SelfServiceJsonSchemaService.WithJsonSchemaRepository(_mock.Object).Build();

        var capabilityService = A.CapabilityApplicationService
            .WithCapabilityRepository(capabilityRepository)
            .WithSelfServiceJsonSchemaService(sut)
            .Build();

        await capabilityService.SetJsonMetadata(newCapability.Id, TestJsonMetadata);

        var capabilityWithJsonMetadata = await capabilityRepository.Get(newCapability.Id);
        Assert.Equal(TestJsonMetadata, capabilityWithJsonMetadata.JsonMetadata);
        Assert.Equal(1, capabilityWithJsonMetadata.JsonMetadataSchemaVersion);
    }

    [Fact]
    public async Task fails_to_set_json_metadata_with_schema_check()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();
        var capabilityRepository = A.CapabilityRepository.WithDbContext(dbContext).Build();

        var newCapability = A.Capability.Build();
        await capabilityRepository.Add(newCapability);
        await dbContext.SaveChangesAsync();

        _mock
            .Setup(x => x.GetLatestSchema(SelfServiceJsonSchemaObjectId.Capability))
            .ReturnsAsync(
                () => new SelfServiceJsonSchema(1, SelfServiceJsonSchemaObjectId.Capability, SchemaWithRequiredField)
            );
        var sut = A.SelfServiceJsonSchemaService.WithJsonSchemaRepository(_mock.Object).Build();

        var capabilityService = A.CapabilityApplicationService
            .WithCapabilityRepository(capabilityRepository)
            .WithSelfServiceJsonSchemaService(sut)
            .Build();

        await Assert.ThrowsAsync<InvalidJsonMetadataException>(
            () => capabilityService.SetJsonMetadata(newCapability.Id, InvalidJsonMetadata)
        );
    }
}
