using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Api;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class ApiResourceFactoryBuilder
{
    private SelfService.Domain.Services.IAuthorizationService _authorizationService;
    private IMembershipQuery _membershipQuery;

    public ApiResourceFactoryBuilder()
    {
        Mock.Of<IAuthorizationService>();
        _authorizationService = Dummy.Of<SelfService.Domain.Services.IAuthorizationService>();
        _membershipQuery = Dummy.Of<IMembershipQuery>();
    }

    public ApiResourceFactoryBuilder WithAuthorizationService(IAuthorizationService authorizationService)
    {
        return this;
    }

    public ApiResourceFactoryBuilder WithDomainAuthorizationService(SelfService.Domain.Services.IAuthorizationService domainAuthorizationService)
    {
        _authorizationService = domainAuthorizationService;
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
            membershipQuery: _membershipQuery
        );
    }

    public static implicit operator ApiResourceFactory(ApiResourceFactoryBuilder builder)
        => builder.Build();
}