using Microsoft.AspNetCore.Mvc.ModelBinding;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api;

namespace SelfService.Tests.Infrastructure.Api;

public class TestRequestParser
{
    [Fact]
    public void can_parse_valid()
    {
        string validCapabilityId = "valid";
        Assert.True(CapabilityId.TryParse(validCapabilityId, out _));
        string validKafkaTopicId = Guid.NewGuid().ToString();
        Assert.True(KafkaTopicId.TryParse(validKafkaTopicId, out _));

        ModelStateDictionary dictionary = new ModelStateDictionary();

        RequestParserRegistry
            .StringToValueParser(dictionary)
            .Parse<CapabilityId, KafkaTopicId>(validCapabilityId, validKafkaTopicId);
        Assert.True(dictionary.IsValid);
    }

    [Fact]
    public void fails_on_invalid()
    {
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

    public class TestId : ValueObjectGuid<TestId>
    {
        public TestId(Guid newGuid)
            : base(newGuid) { }
    }

    [Fact]
    public void call_fall_back()
    {
        string validTestId = Guid.NewGuid().ToString();
        Assert.True(TestId.TryParse(validTestId, out _));
        var dictionary = new ModelStateDictionary();
        RequestParserRegistry.StringToValueParser(dictionary).Parse<TestId>(validTestId);
        Assert.True(dictionary.IsValid);
        Assert.Equal(0, dictionary.ErrorCount);
    }
}
