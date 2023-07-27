using SelfService.Domain.Models;

namespace SelfService.Tests.Domain.Models;

public class TestMessageType
{
    [Theory]
    [MemberData(nameof(ValidInputValues))]
    public void parse_returns_expected_when_parsing_valid_input(string validInput)
    {
        var sut = MessageType.Parse(validInput);
        Assert.Equal(validInput, sut.ToString());
    }

    [Theory]
    [MemberData(nameof(ValidInputValues))]
    public void try_parse_returns_expected_result_when_parsing_valid_input(string validInput)
    {
        var result = MessageType.TryParse(validInput, out _);
        Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(ValidInputValues))]
    public void try_parse_returns_expected_instance_when_parsing_valid_input(string validInput)
    {
        MessageType.TryParse(validInput, out var result);
        Assert.Equal(validInput, result);
    }

    [Theory]
    [InlineData("FOO-BAR")]
    [InlineData("FOO_BAR")]
    public void try_parse_returns_expected_lowercased_instance_when_parsing_valid_input(string validInput)
    {
        MessageType.TryParse(validInput, out var result);
        Assert.Equal(validInput.ToLower(), result);
    }

    [Theory]
    [InlineData("FOO-BAR")]
    [InlineData("FOO_BAR")]
    public void parse_returns_expected_lowercased_instance_when_parsing_valid_input(string validInput)
    {
        var result = MessageType.Parse(validInput);
        Assert.Equal(validInput.ToLower(), result);
    }

    [Theory]
    [MemberData(nameof(InvalidInputValues))]
    public void try_parse_returns_expected_result_when_parsing_invalid_input(string? invalidInput)
    {
        var result = MessageType.TryParse(invalidInput, out _);
        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(InvalidInputValues))]
    public void try_parse_returns_expected_instance_when_parsing_invalid_input(string? invalidInput)
    {
        MessageType.TryParse(invalidInput, out var result);
        Assert.Null(result);
    }

    [Theory]
    [MemberData(nameof(InvalidInputValues))]
    public void parse_throws_expected_exception_on_invalid_input(string? invalidInput)
    {
        Assert.Throws<FormatException>(() => MessageType.Parse(invalidInput));
    }


    #region helpers

    public static IEnumerable<object[]> ValidInputValues
    {
        get
        {
            var values = new object[]
            {
                "foo",
                "bar",
                "foo-bar",
                "foo_bar",
                "1-2",
                "1_2",
            };

            return values.Select(x => new object[] {x});
        }
    }

    public static IEnumerable<object[]> InvalidInputValues
    {
        get
        {
            var values = new object[]
            {
                null!,
                "",
                " ",
                "   ",
                "foo ",
                " foo",
                " foo ",
                "foo bar",
                "foo_",
                "foo-",
                "_foo",
                "-foo",
                "foo!",
                "fo(o)",
                "fo[o]",
                "f@@",
            };

            return values.Select(x => new object[] {x});
        }
    }

    #endregion
}