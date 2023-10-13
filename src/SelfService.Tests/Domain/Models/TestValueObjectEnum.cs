using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence.Converters;
using Xunit.Abstractions;

namespace SelfService.Tests.Domain.Models;

public class TestValueObjectEnum
{
    private class DummyEnum : ValueObjectEnum<DummyEnum>
    {
        public static readonly DummyEnum Value1 = new("Value1");
        public static readonly DummyEnum Value2 = new("Value2");

        private DummyEnum(string value)
            : base(value) { }
    }

    public static IEnumerable<object[]> ValidInputValues =>
        new object[] { DummyEnum.Value1.ToString(), DummyEnum.Value2.ToString() }.Select(x => new[] { x });

    public static IEnumerable<object[]> InvalidInputValues =>
        new object[] { "", "val", "value1", "value2", "value3" }.Select(x => new[] { x });

    [Theory]
    [MemberData(nameof(ValidInputValues))]
    public void parse_returns_expected_when_parsing_valid_input(string validInput)
    {
        var sut = DummyEnum.Parse(validInput);
        Assert.Equal(validInput, sut.ToString());
    }

    [Theory]
    [MemberData(nameof(InvalidInputValues))]
    public void parse_returns_expected_when_parsing_invalid_input(string validInput)
    {
        Assert.Throws<FormatException>(() =>
        {
            DummyEnum.Parse(validInput);
        });
    }

    [Fact]
    public void enum_converter_can_convert_back_and_forth()
    {
        var converter = new ValueObjectEnumConverter<DummyEnum>();
        var value = DummyEnum.Value1;
        var converted = converter.ConvertToProvider(value);
        var convertedBack = converter.ConvertFromProvider(converted);
        Assert.Equal(value, convertedBack);
    }
}
