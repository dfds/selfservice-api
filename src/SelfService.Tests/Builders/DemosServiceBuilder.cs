using Microsoft.Extensions.Logging;
using Moq;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class DemosServiceBuilder
{
    private IDemosRepository _demosRepository;

    public DemosServiceBuilder()
    {
        _demosRepository = Dummy.Of<IDemosRepository>();
    }

    public DemosServiceBuilder WithDemosRepository(IDemosRepository demosRepository)
    {
        _demosRepository = demosRepository;
        return this;
    }

    public DemosService Build()
    {
        return new DemosService(Mock.Of<ILogger<DemosService>>(), _demosRepository);
    }

    public static implicit operator DemosService(DemosServiceBuilder builder) => builder.Build();
}
