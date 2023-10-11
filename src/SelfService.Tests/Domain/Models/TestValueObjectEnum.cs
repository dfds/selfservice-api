using SelfService.Domain.Models;
using Xunit.Abstractions;

namespace SelfService.Tests.Domain.Models;

public class TestValueObjectEnum
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TestValueObjectEnum(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private class DummyEnum : ValueObjectEnum
    {
        public static readonly ValueObjectEnum Value1 = Create("Value1");
        public static readonly ValueObjectEnum Value2 = Create("Value2");

        protected DummyEnum(string value)
            : base(value) { }
    }

    [Theory]
    [MemberData(nameof(ValidInputValues))]
    public void parse_returns_expected_when_parsing_valid_input(string validInput)
    {
        var sut = DummyEnum.Parse(validInput);
        Assert.Equal(validInput, sut.ToString());
    }

    public static IEnumerable<object[]> ValidInputValues
    {
        get
        {
            var values = new object[] { "Value1", "Value2" };

            return values.Select(x => new object[] { x });
        }
    }
}
