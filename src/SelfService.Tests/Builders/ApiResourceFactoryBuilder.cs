using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;
using SelfService.Infrastructure.Api;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class ApiResourceFactoryBuilder
{
    private IAuthorizationService _authorizationService;
    private SelfService.Domain.Services.IAuthorizationService _domainAuthorizationService;

    public ApiResourceFactoryBuilder()
    {
        _authorizationService = Mock.Of<IAuthorizationService>();
        _domainAuthorizationService = Dummy.Of<SelfService.Domain.Services.IAuthorizationService>();
    }

    public ApiResourceFactoryBuilder WithAuthorizationService(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
        return this;
    }

    public ApiResourceFactoryBuilder WithDomainAuthorizationService(SelfService.Domain.Services.IAuthorizationService domainAuthorizationService)
    {
        _domainAuthorizationService = domainAuthorizationService;
        return this;
    }

    public ApiResourceFactory Build()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock
            .SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext()
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "foo")
                }))
            });

        return new ApiResourceFactory(
            httpContextAccessor: httpContextAccessorMock.Object,
            linkGenerator: Mock.Of<LinkGenerator>(),
            authorizationService: _authorizationService,
            domainAuthorizationService: _domainAuthorizationService
        );
    }

    public static implicit operator ApiResourceFactory(ApiResourceFactoryBuilder builder)
        => builder.Build();
}