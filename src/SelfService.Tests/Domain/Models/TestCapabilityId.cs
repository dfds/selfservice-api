using SelfService.Domain.Models;

namespace SelfService.Tests.Domain.Models;

public class TestCapabilityId
{
    [Theory]
    [InlineData("foo", "foo")]
    [InlineData("FOO", "foo")]
    [InlineData("foo bar", "foo-bar")]
    [InlineData("foo       bar", "foo-bar")]
    [InlineData("foo-", "foo")]
    [InlineData("foo--bar", "foo-bar")]
    [InlineData("-foo", "foo")]
    [InlineData(" foo", "foo")]
    [InlineData("f99", "f99")]
    [InlineData("foo_bar", "foo-bar")]
    [InlineData("foo!!", "foo")]
    [InlineData("fo@@@o", "foo")]
    [InlineData("fææ", "faeae")]
    [InlineData("føø", "foeoe")]
    [InlineData("fåå", "faaaa")]
    [InlineData("thisisaverylongstringwithmorethanalotofcharacters", "thisisaverylongstringw")]
    public void returns_expected_value(string input, string expected)
    {
        var result = CapabilityId.CreateFrom(input);

        var namePart = result.ToString()[0..^6];
        var randomPart = result.ToString()[^6..];

        Assert.Equal(expected, namePart);
        Assert.Matches("-[a-z]{5}", randomPart);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData(null)]
    [InlineData("ððð")]
    [InlineData("---")]
    public void handles_bad_input(string input)
    {
        Assert.Throws<FormatException>(() => CapabilityId.CreateFrom(input));
    }

    [Fact]
    public void check_uniqueness_of_suffix()
    {
        var capabilityIdOne = CapabilityId.CreateFrom("bar");
        var capabilityIdTwo = CapabilityId.CreateFrom("bar");

        Assert.NotEqual(capabilityIdOne, capabilityIdTwo);
    }
}
