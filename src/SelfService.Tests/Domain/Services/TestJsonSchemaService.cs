using Moq;
using SelfService.Domain.Models;

namespace SelfService.Tests.Domain.Services;

public class TestJsonSchemaService
{
    private readonly Mock<ISelfServiceJsonSchemaRepository> _mock = new();

    [Fact]
    public async Task parse_empty_json_schema()
    {
        _mock.Setup(x => x.GetLatestSchema(SelfServiceJsonSchemaObjectId.Capability)).ReturnsAsync(() => null);
        var sut = A.SelfServiceJsonSchemaService.WithJsonSchemaRepository(_mock.Object).Build();

        var resultWhenEmpty = await sut.ValidateJsonMetadata(SelfServiceJsonSchemaObjectId.Capability, "");
        Assert.True(resultWhenEmpty.IsValid());
        Assert.Equal(ValidateJsonMetadataResultCode.SuccessNoSchema, resultWhenEmpty.ResultCode);

        var resultWhenEmptyJsonObject = await sut.ValidateJsonMetadata(SelfServiceJsonSchemaObjectId.Capability, "{}");
        Assert.True(resultWhenEmptyJsonObject.IsValid());
        Assert.Equal(ValidateJsonMetadataResultCode.SuccessNoSchema, resultWhenEmpty.ResultCode);

        var resultWhenNotEmptyJsonMetadata = await sut.ValidateJsonMetadata(
            SelfServiceJsonSchemaObjectId.Capability,
            """{"foo": "bar"}"""
        );
        Assert.False(resultWhenNotEmptyJsonMetadata.IsValid());
        Assert.Equal(ValidateJsonMetadataResultCode.Error, resultWhenNotEmptyJsonMetadata.ResultCode);
    }

    [Fact]
    public async Task parse_json_with_valid_schema_with_no_required_fields_but_additional_properties_not_allowed()
    {
        string jsonSchema = """
                            {
                            "$id": "https://example.com/person.schema.json",
                            "$schema": "https://json-schema.org/draft/2020-12/schema",
                            "title": "Person",
                            "additionalProperties": false
                            }
                            """;

        _mock
            .Setup(x => x.GetLatestSchema(SelfServiceJsonSchemaObjectId.Capability))
            .ReturnsAsync(() => new SelfServiceJsonSchema(1, SelfServiceJsonSchemaObjectId.Capability, jsonSchema));

        var sut = A.SelfServiceJsonSchemaService.WithJsonSchemaRepository(_mock.Object).Build();

        // No required fields, so allowed to be empty
        var resultWhenEmpty = await sut.ValidateJsonMetadata(SelfServiceJsonSchemaObjectId.Capability, "");
        Assert.True(resultWhenEmpty.IsValid());
        Assert.Equal(ValidateJsonMetadataResultCode.SuccessSchemaHasNoRequiredFields, resultWhenEmpty.ResultCode);

        // No required fields, so allowed to be empty
        var resultWhenEmptyJsonObject = await sut.ValidateJsonMetadata(SelfServiceJsonSchemaObjectId.Capability, "{}");
        Assert.True(resultWhenEmptyJsonObject.IsValid());
        Assert.Equal(ValidateJsonMetadataResultCode.SuccessSchemaHasNoRequiredFields, resultWhenEmpty.ResultCode);

        var resultWhenIncorrectJsonMetadata = await sut.ValidateJsonMetadata(
            SelfServiceJsonSchemaObjectId.Capability,
            """{"foo": "bar"}"""
        );
        Assert.False(resultWhenIncorrectJsonMetadata.IsValid());
        Assert.Equal(ValidateJsonMetadataResultCode.Error, resultWhenIncorrectJsonMetadata.ResultCode);
    }

    [Fact]
    public async Task parse_json_with_valid_schema_with_required_fields()
    {
        string jsonSchema = """
                            {
                                "$id": "https://example.com/person.schema.json",
                                "$schema": "https://json-schema.org/draft/2020-12/schema",
                                "required": ["firstName", "hasLastName"],
                                "properties": {
                                  "firstName": {
                                    "type": "string"
                                  },
                                  "hasLastName": {
                                    "type": "boolean"
                                  }
                                },
                                "additionalProperties": false
                            }
                            """;

        _mock
            .Setup(x => x.GetLatestSchema(SelfServiceJsonSchemaObjectId.Capability))
            .ReturnsAsync(() => new SelfServiceJsonSchema(1, SelfServiceJsonSchemaObjectId.Capability, jsonSchema));

        var sut = A.SelfServiceJsonSchemaService.WithJsonSchemaRepository(_mock.Object).Build();

        // Not allowed to be empty, when we have required fields
        var resultWhenEmpty = await sut.ValidateJsonMetadata(SelfServiceJsonSchemaObjectId.Capability, "");
        Assert.False(resultWhenEmpty.IsValid());
        Assert.Equal(ValidateJsonMetadataResultCode.Error, resultWhenEmpty.ResultCode);

        // Not allowed to be empty, when we have required fields
        var resultWhenEmptyJsonObject = await sut.ValidateJsonMetadata(SelfServiceJsonSchemaObjectId.Capability, "{}");
        Assert.False(resultWhenEmptyJsonObject.IsValid());
        Assert.Equal(ValidateJsonMetadataResultCode.Error, resultWhenEmpty.ResultCode);

        var resultWhenIncorrectJsonMetadata = await sut.ValidateJsonMetadata(
            SelfServiceJsonSchemaObjectId.Capability,
            """{"foo": "bar"}"""
        );
        Assert.False(resultWhenIncorrectJsonMetadata.IsValid());
        Assert.Equal(ValidateJsonMetadataResultCode.Error, resultWhenIncorrectJsonMetadata.ResultCode);

        var resultWhenPartiallyCorrectJsonMetadata = await sut.ValidateJsonMetadata(
            SelfServiceJsonSchemaObjectId.Capability,
            """
                {
                  "firstName": "John"
                }
                """
        );
        Assert.False(resultWhenPartiallyCorrectJsonMetadata.IsValid());
        Assert.Equal(ValidateJsonMetadataResultCode.Error, resultWhenPartiallyCorrectJsonMetadata.ResultCode);

        var resultWhenCorrectJsonMetadata = await sut.ValidateJsonMetadata(
            SelfServiceJsonSchemaObjectId.Capability,
            """
                {
                  "firstName": "John",
                  "hasLastName": true
                }
                """
        );
        Assert.True(resultWhenCorrectJsonMetadata.IsValid());
        Assert.Equal(ValidateJsonMetadataResultCode.SuccessValidJsonMetadata, resultWhenCorrectJsonMetadata.ResultCode);
    }
}
