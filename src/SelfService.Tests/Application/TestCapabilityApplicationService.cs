using Moq;
using SelfService.Application;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.Application;

public class TestCapabilityApplicationService
{
    private const string JsonMetadata = """
                                            {
                                                "foo": "bar"
                                            }
                                        """;

    private const string InvalidJsonMetadata = """
                                                   {
                                                       "not": "valid"
                                                   }
                                               """;

    private const string SchemaEmpty = """{}""";

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

    private SelfServiceJsonSchema CreateSchema(int version, string schemaString)
    {
        return new SelfServiceJsonSchema(version, SelfServiceJsonSchemaObjectId.Capability, schemaString);
    }

    /// <summary>
    /// Creates a capability, repository and service for testing json metadata
    /// </summary>
    /// <param name="schema">The schema you wish to use for the test</param>
    /// <param name="dbContext">Must be passed in to avoid being Disposed</param>
    private async Task<(
        Capability,
        CapabilityRepository repo,
        CapabilityApplicationService capabilityService
    )> SetupCapabilityMetadataTesting(SelfServiceJsonSchema schema, SelfServiceDbContext dbContext)
    {
        var repo = A.CapabilityRepository.WithDbContext(dbContext).Build();

        var newCapability = A.Capability.Build();
        await repo.Add(newCapability);
        await dbContext.SaveChangesAsync();

        _mock.Setup(x => x.GetLatestSchema(SelfServiceJsonSchemaObjectId.Capability)).ReturnsAsync(() => schema);
        var sut = A.SelfServiceJsonSchemaService.WithJsonSchemaRepository(_mock.Object).Build();
        var confService = A.ConfigurationLevelService.WithCapabilityRepository(repo).Build();
        var capabilityService = A.CapabilityApplicationService
            .WithCapabilityRepository(repo)
            .WithSelfServiceJsonSchemaService(sut)
            .WithConfigurationLevelService(confService)
            .Build();
        return (newCapability, repo, capabilityService);
    }

    [Fact]
    public async Task can_set_json_metadata()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var (capability, repo, service) = await SetupCapabilityMetadataTesting(CreateSchema(0, SchemaEmpty), dbContext);

        await service.SetJsonMetadata(capability.Id, JsonMetadata);
        var capabilityWithJsonMetadata = await repo.Get(capability.Id);
        Assert.Equal(JsonMetadata, capabilityWithJsonMetadata.JsonMetadata);
        Assert.Equal(0, capabilityWithJsonMetadata.JsonMetadataSchemaVersion);
    }

    [Fact]
    public async Task can_set_json_metadata_with_schema_check()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var (capability, repo, service) = await SetupCapabilityMetadataTesting(
            CreateSchema(1, SchemaWithRequiredField),
            dbContext
        );

        await service.SetJsonMetadata(capability.Id, JsonMetadata);

        var capabilityWithJsonMetadata = await repo.Get(capability.Id);
        Assert.Equal(JsonMetadata, capabilityWithJsonMetadata.JsonMetadata);
        Assert.Equal(1, capabilityWithJsonMetadata.JsonMetadataSchemaVersion);
    }

    [Fact]
    public async Task fails_to_set_json_metadata_with_schema_check()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var (capability, _, service) = await SetupCapabilityMetadataTesting(
            CreateSchema(1, SchemaWithRequiredField),
            dbContext
        );

        await Assert.ThrowsAsync<InvalidJsonMetadataException>(
            () => service.SetJsonMetadata(capability.Id, InvalidJsonMetadata)
        );
    }

    [Fact]
    public async Task fails_if_non_required_are_set()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var (capability, _, service) = await SetupCapabilityMetadataTesting(
            CreateSchema(1, SchemaWithRequiredField),
            dbContext
        );
        Assert.False(await service.DoesOnlyModifyRequiredProperties(InvalidJsonMetadata, capability.Id));
        Assert.True(await service.DoesOnlyModifyRequiredProperties(JsonMetadata, capability.Id));
    }

    [Fact]
    public async Task fails_if_no_required_in_schema()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var (capability, _, service) = await SetupCapabilityMetadataTesting(CreateSchema(0, SchemaEmpty), dbContext);
        Assert.False(await service.DoesOnlyModifyRequiredProperties(InvalidJsonMetadata, capability.Id));
        Assert.False(await service.DoesOnlyModifyRequiredProperties(JsonMetadata, capability.Id));
    }
}
