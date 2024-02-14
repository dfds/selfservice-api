using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.Application;

public class TestMessageContractValidation
{
    private string BuildProperty(string name, string type, string example)
    {
        return $$"""
                 "{{name}}": {
                       "type": "{{type}}",
                       "examples": [
                         "{{example}}"
                       ]
                    }
                 """;
    }

    private string BuildData(string[] properties, bool additionalProperties)
    {
        return $$"""
                 {
                   "type": "object",
                   "properties": {{{string.Join(",", properties)}}},
                   "additionalProperties": {{additionalProperties.ToString().ToLower()}}
                 }
                 """;
    }

    private string GetSchema(int version, string data)
    {
        return $$"""
                 {
                   "type": "object",
                   "properties": {
                    "schemaVersion":{
                        "type": "integer",
                        "const":{{version}}
                    },
                     "messageId": {
                       "type": "string",
                       "examples": [
                         "<123>"
                       ]
                     },
                     "type": {
                       "type": "string",
                       "examples": [
                         "dfds-envelope"
                       ]
                     },
                     "data": {{data}}
                   },
                   "required": [
                     "messageId",
                     "type",
                     "data",
                     "schemaVersion"
                   ]
                 }
                 """;
    }

    [Fact]
    public async Task succeeds_on_valid_first_schema_version()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var testKafkaTopic = A.KafkaTopic.Build();
        KafkaTopicRepository kafkaTopicRepository = new(dbContext);
        MessageContractRepository messageContractRepository = new(dbContext);
        await kafkaTopicRepository.Add(testKafkaTopic);
        await dbContext.SaveChangesAsync();

        var kafkaTopicApplicationService = A.KafkaTopicApplicationService
            .WithKafkaTopicRepository(kafkaTopicRepository)
            .WithMessageContractRepository(messageContractRepository)
            .Build();

        var schemaString = GetSchema(1, BuildData(new[] { BuildProperty("someTest", "integer", "1") }, false));
        var testSchema = MessageContractSchema.Parse(schemaString);
        await kafkaTopicApplicationService.ValidateRequestForCreatingNewContract(
            testKafkaTopic.Id,
            MessageType.Parse("test"),
            testSchema
        );
    }

    private async Task SchemaWithTwoPropertiesThenDeleting(
        bool openContentModelFirstSchema,
        bool openContentModelSecondSchema
    )
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        ;
        var firstValidSchema = GetSchema(
            1,
            BuildData(
                new[]
                {
                    BuildProperty("someTest", "integer", "1"),
                    BuildProperty("someNewProperty", "string", "hello")
                },
                openContentModelFirstSchema
            )
        );

        var testKafkaTopic = A.KafkaTopic.Build();
        var testMessageContract = A.MessageContract
            .WithKafkaTopicId(testKafkaTopic.Id)
            .WithSchema(firstValidSchema)
            .WithSchemaVersion(1)
            .WithType(MessageType.Parse("test"))
            .WithStatus(MessageContractStatus.Provisioned)
            .Build();

        KafkaTopicRepository kafkaTopicRepository = new(dbContext);
        MessageContractRepository messageContractRepository = new(dbContext);
        await kafkaTopicRepository.Add(testKafkaTopic);
        await messageContractRepository.Add(testMessageContract);
        await dbContext.SaveChangesAsync();

        var kafkaTopicApplicationService = A.KafkaTopicApplicationService
            .WithKafkaTopicRepository(kafkaTopicRepository)
            .WithMessageContractRepository(messageContractRepository)
            .Build();

        var secondValidEvolution = GetSchema(
            2,
            BuildData(new[] { BuildProperty("someTest", "integer", "1") }, openContentModelSecondSchema)
        );
        var testSchema = MessageContractSchema.Parse(secondValidEvolution);
        await kafkaTopicApplicationService.ValidateRequestForCreatingNewContract(
            testKafkaTopic.Id,
            testMessageContract.MessageType,
            testSchema
        );
    }

    private async Task SchemaOnePropertyThenAddingAnother(
        bool openContentModelFirstSchema,
        bool openContentModelSecondSchema
    )
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        List<string> properties = new List<string>() { BuildProperty("someTest", "integer", "1") };
        var firstValidSchema = GetSchema(1, BuildData(properties.ToArray(), openContentModelFirstSchema));

        var testKafkaTopic = A.KafkaTopic.Build();
        var testMessageContract = A.MessageContract
            .WithKafkaTopicId(testKafkaTopic.Id)
            .WithSchema(firstValidSchema)
            .WithSchemaVersion(1)
            .WithType(MessageType.Parse("test"))
            .WithStatus(MessageContractStatus.Provisioned)
            .Build();

        KafkaTopicRepository kafkaTopicRepository = new(dbContext);
        MessageContractRepository messageContractRepository = new(dbContext);
        await kafkaTopicRepository.Add(testKafkaTopic);
        await messageContractRepository.Add(testMessageContract);
        await dbContext.SaveChangesAsync();

        var kafkaTopicApplicationService = A.KafkaTopicApplicationService
            .WithKafkaTopicRepository(kafkaTopicRepository)
            .WithMessageContractRepository(messageContractRepository)
            .Build();

        properties.Add(BuildProperty("someNewProperty", "string", "hello"));
        var secondValidEvolution = GetSchema(2, BuildData(properties.ToArray(), openContentModelSecondSchema));
        var testSchema = MessageContractSchema.Parse(secondValidEvolution);
        await kafkaTopicApplicationService.ValidateRequestForCreatingNewContract(
            testKafkaTopic.Id,
            testMessageContract.MessageType,
            testSchema
        );
    }

    [Fact]
    public async Task closed_content_model_succeeds_on_evolution_adding_property()
    {
        await SchemaOnePropertyThenAddingAnother(false, false);
    }

    [Fact]
    public async Task closed_content_model_fails_on_evolution_remove_property()
    {
        await Assert.ThrowsAsync<InvalidMessageContractRequestException>(
            async () => await SchemaWithTwoPropertiesThenDeleting(false, false)
        );
    }

    [Fact]
    public async Task open_content_model_succeeds_on_evolution_removing_property()
    {
        await SchemaWithTwoPropertiesThenDeleting(true, true);
    }

    [Fact]
    public async Task open_content_model_failed_on_evolution_adding_property()
    {
        await Assert.ThrowsAsync<InvalidMessageContractRequestException>(
            async () => await SchemaOnePropertyThenAddingAnother(true, true)
        );
    }

    [Fact]
    async Task fails_when_going_from_open_content_model_to_closed()
    {
        await Assert.ThrowsAsync<InvalidMessageContractRequestException>(
            async () => await SchemaOnePropertyThenAddingAnother(true, false)
        );
    }

    [Fact]
    async Task succeeds_when_going_from_closed_content_model_to_opened()
    {
        await SchemaOnePropertyThenAddingAnother(false, true);
    }

    [Fact]
    async Task fails_when_changing_property_type()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        List<string> properties = new List<string>() { BuildProperty("someTest", "integer", "1") };
        var firstValidSchema = GetSchema(1, BuildData(properties.ToArray(), false));

        var testKafkaTopic = A.KafkaTopic.Build();
        var testMessageContract = A.MessageContract
            .WithKafkaTopicId(testKafkaTopic.Id)
            .WithSchema(firstValidSchema)
            .WithSchemaVersion(1)
            .WithType(MessageType.Parse("test"))
            .WithStatus(MessageContractStatus.Provisioned)
            .Build();

        KafkaTopicRepository kafkaTopicRepository = new(dbContext);
        MessageContractRepository messageContractRepository = new(dbContext);
        await kafkaTopicRepository.Add(testKafkaTopic);
        await messageContractRepository.Add(testMessageContract);
        await dbContext.SaveChangesAsync();

        var kafkaTopicApplicationService = A.KafkaTopicApplicationService
            .WithKafkaTopicRepository(kafkaTopicRepository)
            .WithMessageContractRepository(messageContractRepository)
            .Build();

        properties.Add(BuildProperty("someTest", "boolean", "false"));
        var secondValidEvolution = GetSchema(2, BuildData(properties.ToArray(), false));
        var testSchema = MessageContractSchema.Parse(secondValidEvolution);
        await Assert.ThrowsAsync<InvalidMessageContractRequestException>(
            async () =>
                await kafkaTopicApplicationService.ValidateRequestForCreatingNewContract(
                    testKafkaTopic.Id,
                    testMessageContract.MessageType,
                    testSchema
                )
        );
    }

    [Fact]
    void can_detect_correct_dfds_envelope()
    {
        var schemaString = GetSchema(1, BuildData(new[] { BuildProperty("someTest", "integer", "1") }, false));
        var testSchema = MessageContractSchema.Parse(schemaString);
        testSchema.ValidateSchemaEnvelope();
    }

    [Fact]
    void fails_on_incorrect_dfds_envelope()
    {
        string CreateSchemaWithRequired(string[] required)
        {
            return $$"""
                     {
                       "type": "object",
                       "properties": {
                        "schemaVersion":{
                            "type": "integer",
                            "const": 1
                        },
                         "messageId": {
                           "type": "string",
                           "examples": [
                             "<123>"
                           ]
                         },
                         "type": {
                           "type": "string",
                           "examples": [
                             "dfds-envelope"
                           ]
                         },
                         "data": {
                             "some_data": {
                               "type": "string",
                               "examples": [
                                 "dfds-envelope"
                               ]
                             }
                         }
                       },
                       "required": [
                          {{string.Join(",", required.Select(p => $"\"{p}\""))}}
                       ]
                     }
                     """;
        }

        void AssertThrows(string[] required)
        {
            Assert.Throws<InvalidMessageContractEnvelopeException>(
                () => MessageContractSchema.Parse(CreateSchemaWithRequired(required)).ValidateSchemaEnvelope()
            );
        }

        AssertThrows(new[] { "schemaVersion", "type", "data" });
        AssertThrows(new[] { "schemaVersion", "type", "messageId" });
        AssertThrows(new[] { "schemaVersion", "data", "messageId" });
        AssertThrows(new[] { "type", "data", "messageId" });
        AssertThrows(new[] { "schemaVersion", "type" });
        AssertThrows(new[] { "schemaVersion", "data" });
        AssertThrows(new[] { "schemaVersion", "messageId" });
        AssertThrows(new[] { "type", "data" });
        AssertThrows(new[] { "type", "messageId" });
        AssertThrows(new[] { "data", "messageId" });
        AssertThrows(new[] { "schemaVersion" });
        AssertThrows(new[] { "type" });
        AssertThrows(new[] { "data" });
        AssertThrows(new[] { "messageId" });
        AssertThrows(new string[] { });
    }
}
