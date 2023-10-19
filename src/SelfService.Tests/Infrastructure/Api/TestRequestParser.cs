using Microsoft.AspNetCore.Mvc.ModelBinding;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api;

namespace SelfService.Tests.Infrastructure.Api;

public class TestRequestParser
{
    [Fact]
    public void can_parse_valid()
    {
        // Unknown when static constructor is called, so we need to call it explicitly
        RequestParserRegistry.Init();

        string validCapabilityId = "valid";
        Assert.True(CapabilityId.TryParse(validCapabilityId, out _));
        string validKafkaTopicId = Guid.NewGuid().ToString();
        Assert.True(KafkaTopicId.TryParse(validKafkaTopicId, out _));

        ModelStateDictionary dictionary = new ModelStateDictionary();

        var (_, _) = RequestParserRegistry
            .StringToValueParser(dictionary)
            .Parse<CapabilityId, KafkaTopicId>(validCapabilityId, validKafkaTopicId);
        Assert.True(dictionary.IsValid);
    }

    [Fact]
    public void fails_on_invalid()
    {
        // Unknown when static constructor is called, so we need to call it explicitly
        RequestParserRegistry.Init();

        string invalidCapabilityId = "(+_+)";
        Assert.False(CapabilityId.TryParse(invalidCapabilityId, out _));
        string validKafkaTopicId = Guid.NewGuid().ToString();
        Assert.True(KafkaTopicId.TryParse(validKafkaTopicId, out _));
        string invalidKafkaTopicId = "invalid";
        Assert.False(KafkaTopicId.TryParse(invalidKafkaTopicId, out _));

        ModelStateDictionary dictionary = new ModelStateDictionary();
        RequestParserRegistry
            .StringToValueParser(dictionary)
            .Parse<CapabilityId, KafkaTopicId, KafkaTopicId>(
                invalidCapabilityId,
                validKafkaTopicId,
                invalidCapabilityId
            );
        Assert.False(dictionary.IsValid);
        Assert.Equal(2, dictionary.ErrorCount);
    }
}
