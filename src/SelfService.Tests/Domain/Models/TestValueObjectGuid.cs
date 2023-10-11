using SelfService.Domain.Models;

namespace SelfService.Tests.Domain.Models;

public class TestValueObjectGuid
{
    private class DummyEnum : ValueObjectGuid<DummyEnum>
    {
        public static readonly DummyEnum Value1 = new("Value1");
        public static readonly DummyEnum Value2 = new("Value2");

        private DummyEnum(string value)
            : base(value) { }
    }
}
