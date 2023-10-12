using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence.Converters;

namespace SelfService.Tests.Domain.Models;

public class TestValueObjectGuid
{
    private class DummyValueObjectGuid : ValueObjectGuid<DummyValueObjectGuid>
    {
        protected DummyValueObjectGuid(Guid newGuid)
            : base(newGuid) { }
    }

    // test if dummy value object guids work
    [Fact]
    public void dummy_value_object_guids_work()
    {
        var sut = DummyValueObjectGuid.New();
        Assert.NotNull(sut);
    }

    [Fact]
    public void guid_converter_can_convert_back_and_forth()
    {
        var converter = new ValueObjectGuidConverter<DummyValueObjectGuid>();
        var value = DummyValueObjectGuid.New();
        var converted = converter.ConvertToProvider(value)!;
        var convertedBack = converter.ConvertFromProvider(converted);
        Assert.Equal(value, convertedBack);
    }
}
