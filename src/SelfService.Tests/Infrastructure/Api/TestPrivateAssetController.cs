using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.PrivateAssets;
using SelfService.Tests.Builders;

namespace SelfService.Tests.Infrastructure.Api;

public class TestPrivateAssetController
{
    [Fact]
    public async Task upload_returns_storage_key_and_metadata_for_valid_file()
    {
        var stream = new MemoryStream([1, 2, 3, 4]);
        var file = new FormFile(stream, 0, stream.Length, "file", "banner.png") { Headers = new HeaderDictionary() };
        file.ContentType = "image/png";

        var privateAssetService = new Mock<IPrivateAssetApplicationService>();
        var asset = A
            .PrivateAsset.WithStorageKey("private-assets/abc123/banner.png")
            .WithFileName("banner.png")
            .Build();
        privateAssetService
            .Setup(x =>
                x.Upload(
                    It.IsAny<Guid>(),
                    PrivateAssetOwnerType.EmailCampaign,
                    true,
                    "banner.png",
                    "image/png",
                    4,
                    It.IsAny<Stream>(),
                    It.IsAny<PortalUser>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(asset);
        privateAssetService
            .Setup(x => x.GetPreviewUrl(asset, It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.test/private-assets/abc123/banner.png");

        var authorizationService = new Mock<IAuthorizationService>();
        authorizationService.Setup(x => x.CanCreatePrivateAsset(It.IsAny<PortalUser>())).Returns(true);

        var controller = BuildController(privateAssetService.Object, authorizationService.Object);

        var result = await controller.Upload(
            file,
            Guid.NewGuid(),
            PrivateAssetOwnerType.EmailCampaign,
            true,
            CancellationToken.None
        );

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PrivateAssetUploadResponse>(okResult.Value);

        Assert.Equal("private-assets/abc123/banner.png", response.StorageKey);
        Assert.Equal("banner.png", response.FileName);
        Assert.Equal("image/png", response.MimeType);
        Assert.Equal(asset.Id.ToString(), response.AssetId);
        Assert.Equal("https://example.test/private-assets/abc123/banner.png", response.Url);
    }

    [Fact]
    public async Task upload_rejects_files_that_are_not_supported_images()
    {
        var stream = new MemoryStream([1, 2, 3]);
        var file = new FormFile(stream, 0, stream.Length, "file", "document.txt") { Headers = new HeaderDictionary() };
        file.ContentType = "text/plain";

        var privateAssetService = new Mock<IPrivateAssetApplicationService>();
        var authorizationService = new Mock<IAuthorizationService>();
        authorizationService.Setup(x => x.CanCreatePrivateAsset(It.IsAny<PortalUser>())).Returns(true);

        var controller = BuildController(privateAssetService.Object, authorizationService.Object);

        var result = await controller.Upload(
            file,
            Guid.NewGuid(),
            PrivateAssetOwnerType.EmailCampaign,
            true,
            CancellationToken.None
        );

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Contains("Only PNG", problem.Detail);
        privateAssetService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task upload_requires_private_assets_create_permission()
    {
        var stream = new MemoryStream([1, 2, 3]);
        var file = new FormFile(stream, 0, stream.Length, "file", "banner.png") { Headers = new HeaderDictionary() };
        file.ContentType = "image/png";

        var privateAssetService = new Mock<IPrivateAssetApplicationService>();
        var authorizationService = new Mock<IAuthorizationService>();
        authorizationService.Setup(x => x.CanCreatePrivateAsset(It.IsAny<PortalUser>())).Returns(false);

        var controller = BuildController(privateAssetService.Object, authorizationService.Object);

        var result = await controller.Upload(
            file,
            Guid.NewGuid(),
            PrivateAssetOwnerType.EmailCampaign,
            true,
            CancellationToken.None
        );

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(unauthorized.Value);
        Assert.Contains("private-assets:create", problem.Detail);
        privateAssetService.VerifyNoOtherCalls();
    }

    private static PrivateAssetController BuildController(
        IPrivateAssetApplicationService privateAssetService,
        IAuthorizationService? authorizationService = null
    )
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, "tester@dfds.com"),
                    new Claim(ClaimTypes.Email, "tester@dfds.com"),
                    new Claim("preferred_username", "tester@dfds.com"),
                    new Claim(ClaimTypes.Role, UserRole.CloudEngineer.ToString()),
                ],
                "TestAuthType"
            )
        );

        return new PrivateAssetController(
            privateAssetService,
            authorizationService
                ?? Mock.Of<IAuthorizationService>(x => x.CanCreatePrivateAsset(It.IsAny<PortalUser>()) == true)
        )
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } },
        };
    }
}
