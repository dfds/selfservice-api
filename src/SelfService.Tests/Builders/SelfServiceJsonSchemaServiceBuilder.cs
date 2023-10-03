using Microsoft.Extensions.Logging;
using Moq;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class SelfServiceJsonSchemaServiceBuilder
{
    private ISelfServiceJsonSchemaRepository _selfServiceJsonSchemaRepository;

    public SelfServiceJsonSchemaServiceBuilder()
    {
        _selfServiceJsonSchemaRepository = Dummy.Of<ISelfServiceJsonSchemaRepository>();
    }

    public SelfServiceJsonSchemaServiceBuilder WithJsonSchemaRepository(
        ISelfServiceJsonSchemaRepository selfServiceJsonSchemaService
    )
    {
        _selfServiceJsonSchemaRepository = selfServiceJsonSchemaService;
        return this;
    }

    public SelfServiceJsonSchemaService Build()
    {
        return new SelfServiceJsonSchemaService(
            Mock.Of<ILogger<SelfServiceJsonSchemaService>>(),
            _selfServiceJsonSchemaRepository
        );
    }

    public static implicit operator SelfServiceJsonSchemaService(SelfServiceJsonSchemaServiceBuilder builder) =>
        builder.Build();
}
