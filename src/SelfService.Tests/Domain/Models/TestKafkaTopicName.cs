using System.Text;
using SelfService.Domain.Models;

namespace SelfService.Tests.Domain.Models;

public class TestKafkaTopicName
{
    [Theory]
    [InlineData("foo")]
    [InlineData("foo.bar")]
    [InlineData("foo-bar")]
    [InlineData("foo_bar")]
    [InlineData("foo_1")]
    [InlineData("foo-1")]
    [InlineData("foo.1")]
    public void returns_expected_string_representation_when_created_with_capability_id(string validName)
    {
        var stubCapabilityId = CapabilityId.Parse("foo");
        var sut = KafkaTopicName.CreateFrom(stubCapabilityId, validName);

        Assert.Equal($"foo.{validName}", sut);
    }

    [Fact]
    public void string_representation_is_always_lower_case()
    {
        var stubCapabilityId = CapabilityId.Parse("foo");
        var sut = KafkaTopicName.CreateFrom(stubCapabilityId, "BAR");

        Assert.Equal("foo.bar", sut);
    }

    [Fact]
    public void returns_expected_string_representation_when_created_with_capability_id_and_is_public()
    {
        var stubCapabilityId = CapabilityId.Parse("foo");
        var sut = KafkaTopicName.CreateFrom(stubCapabilityId, "bar", isPublic: true);

        Assert.Equal("pub.foo.bar", sut);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("    ")]
    [InlineData(",")]
    [InlineData(".")]
    [InlineData("-")]
    [InlineData("_")]
    [InlineData("?")]
    //[InlineData("foo.")]
    [InlineData("foo,")]
    [InlineData("foo_")]
    [InlineData("foo-")]
    [InlineData("foo?")]
    [InlineData("foo..bar")]
    [InlineData("foo__bar")]
    [InlineData("foo--bar")]
    //[InlineData("foo.-bar")]
    //[InlineData("foo._bar")]
    //[InlineData("foo-.bar")]
    //[InlineData("foo-_bar")]
    //[InlineData("foo_.bar")]
    //[InlineData("foo_-bar")]
    //[InlineData("foo_-_-_-_-_-_-_-_bar")]
    [InlineData("foo bar")]
    [InlineData("foo bar ")]
    [InlineData(" foo bar")]
    [InlineData("foo(bar)")]
    [InlineData("foo[]!\"#¤%&/()=?`´|")]
    public void throws_expected_exception_when_created_with_invalid_name(string? invalidName)
    {
        var stubCapabilityId = CapabilityId.Parse("foo");
        Assert.Throws<ArgumentException>(() => KafkaTopicName.CreateFrom(stubCapabilityId, invalidName!));
    }

    [Fact]
    public void try_parse_returns_expected_result_when_parsing_valid_input()
    {
        var result = KafkaTopicName.TryParse("foo.bar", out _);
        Assert.True(result);
    }

    [Theory]
    [InlineData("foo.bar")]
    [InlineData("foo.BAR")]
    public void try_parse_returns_expected_instance_when_parsing_valid_input(string validInput)
    {
        var valid = KafkaTopicName.TryParse(validInput, out var result);
        Assert.True(valid);
        Assert.Equal("foo.bar", result);
    }

    [Fact]
    public void try_parse_returns_expected_result_when_parsing_invalid_input()
    {
        var result = KafkaTopicName.TryParse("foo bar", out _);
        Assert.False(result);
    }

    [Fact]
    public void try_parse_returns_expected_instance_when_parsing_invalid_input()
    {
        var valid = KafkaTopicName.TryParse("foo bar", out var result);
        Assert.False(valid);
        Assert.Equal("foo bar", result);
    }

    [Fact]
    public void extract_capability_id_returns_expected()
    {
        var expectedCapabilityId = CapabilityId.Parse("foo");
        var sut = KafkaTopicName.CreateFrom(expectedCapabilityId, "bar");
        var result = sut.ExtractCapabilityId();

        Assert.Equal(expectedCapabilityId, result);
    }
}
