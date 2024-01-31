using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class ConfigurationLevelServiceBuilder
{
    private ICapabilityRepository _capabilityRepository = Dummy.Of<ICapabilityRepository>();

    public ConfigurationLevelServiceBuilder WithCapabilityRepository(ICapabilityRepository capabilityRepository)
    {
        _capabilityRepository = capabilityRepository;
        return this;
    }

    public ConfigurationLevelService Build()
    {
        return new ConfigurationLevelService(_capabilityRepository);
    }

    public static implicit operator ConfigurationLevelService(ConfigurationLevelServiceBuilder builder) =>
        builder.Build();
}
