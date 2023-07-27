using SelfService.Domain.Models;

namespace SelfService.Tests.Domain.Models;

public class TestKafkaTopicRetention
{
    [Theory]
    [MemberData(nameof(ValidInput))]
    public void try_parse_returns_expected_result_when_given_valid_input(string input)
    {
        var result = KafkaTopicRetention.TryParse(input, out _);
        Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(ValidInput))]
    public void try_parse_returns_expected_instance_when_given_valid_input(string input)
    {
        KafkaTopicRetention.TryParse(input, out var result);
        Assert.Equal(input.ToLowerInvariant(), result);
    }

    [Theory]
    [MemberData(nameof(InvalidInput))]
    public void try_parse_returns_expected_result_when_given_invalid_input(string? invalidInput)
    {
        var result = KafkaTopicRetention.TryParse(invalidInput, out _);
        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(InvalidInput))]
    public void try_parse_returns_expected_instance_when_given_invalid_input(string? invalidInput)
    {
        KafkaTopicRetention.TryParse(invalidInput, out var result);
        Assert.Null(result);
    }

    #region helpers

    public static IEnumerable<object[]> ValidInput
    {
        get
        {
            var validValues = new object[]
            {
                "1d",
                "1D",
                "forever",
                "FOREVER",
                "FoReVeR",
                "fOrEvEr",
            };

            return validValues.Select(x => new object[] {x});
        }
    }

    public static IEnumerable<object[]> InvalidInput
    {
        get
        {
            var invalidValues = new object[]
            {
                null!,
                "",
                " ",
                "     ",
                "-",
                "!",
                "1",
                "123",
                "-1",
                "-1d",
                "d1",
                "0d",
                "0D",
                "1.5d",
                "1,5d",
                "1!5d",
                "1-5d",
            };

            return invalidValues.Select(x => new object[] {x});
        }
    }

    #endregion

}