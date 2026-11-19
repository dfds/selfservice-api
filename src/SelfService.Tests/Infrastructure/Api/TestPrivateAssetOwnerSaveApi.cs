using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api;
using SelfService.Infrastructure.Api.Events;
using SelfService.Infrastructure.Api.News;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.Infrastructure.Api;

public class TestPrivateAssetOwnerSaveApi
{
    private static PrivateAsset Asset(PrivateAssetOwnerType ownerType) =>
        new(
            PrivateAssetId.New(),
            "key",
            "image.png",
            "image/png",
            4,
            "tester@dfds.com",
            DateTime.UtcNow,
            null,
            ownerType,
            true
        );

    private static DefaultHttpContext Context() =>
        new()
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.Name, "tester@dfds.com"),
                        new Claim(ClaimTypes.Email, "tester@dfds.com"),
                        new Claim("preferred_username", "tester@dfds.com"),
                        new Claim(ClaimTypes.Role, UserRole.CloudEngineer.ToString()),
                    ],
                    "TestAuthType"
                )
            ),
        };

    private static ApiResourceFactory Resources(HttpContext context, IAuthorizationService authorization) =>
        new(
            new HttpContextAccessor { HttpContext = context },
            Mock.Of<LinkGenerator>(),
            authorization,
            Mock.Of<IMembershipQuery>(),
            Mock.Of<ICapabilityDeletionStatusQuery>(),
            Mock.Of<IAwsAccountIdQuery>()
        );

    [Fact]
    public async Task news_create_and_update_claim_images_and_reject_invalid_claims()
    {
        await using var factory = new InMemoryDatabaseFactory();
        await using var db = await factory.CreateSelfServiceDbContext();
        var first = Asset(PrivateAssetOwnerType.News);
        var second = Asset(PrivateAssetOwnerType.News);
        db.PrivateAssets.AddRange(first, second);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var context = Context();
        var authorization = new Mock<IAuthorizationService>();
        authorization.Setup(service => service.CanCreateNewsItem(It.IsAny<PortalUser>())).ReturnsAsync(true);
        authorization.Setup(service => service.CanUpdateNewsItem(It.IsAny<PortalUser>())).ReturnsAsync(true);
        using var services = TransactionalServiceFactory.Create(db);
        var service = services.GetRequiredService<INewsItemService>();
        var controller = new NewsController(Resources(context, authorization.Object), service, authorization.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context },
        };

        var created = Assert.IsType<NewsItemApiResource>(
            Assert
                .IsType<OkObjectResult>(
                    await controller.CreateNewsItem(
                        new NewsItemCreateRequest
                        {
                            Title = "News",
                            Body = "Body",
                            DueDate = DateTime.UtcNow,
                            AssetIds = [(Guid)first.Id],
                        }
                    )
                )
                .Value
        );
        Assert.Equal(
            (Guid)created.Id,
            (await db.PrivateAssets.AsNoTracking().SingleAsync(asset => asset.Id == first.Id)).OwnerId
        );

        Assert.IsType<OkObjectResult>(
            await controller.UpdateNewsItem(
                created.Id,
                new NewsItemUpdateRequest { Title = "Updated", AssetIds = [(Guid)first.Id, (Guid)second.Id] }
            )
        );
        Assert.Equal(
            2,
            (await new PrivateAssetRepository(db).GetByOwner(PrivateAssetOwnerType.News, created.Id)).Count
        );
        Assert.IsType<BadRequestObjectResult>(
            await controller.UpdateNewsItem(
                created.Id,
                new NewsItemUpdateRequest { Title = "Not saved", AssetIds = [Guid.NewGuid()] }
            )
        );
        db.ChangeTracker.Clear();
        Assert.Equal("Updated", (await db.NewsItems.SingleAsync()).Title);
    }

    [Fact]
    public async Task event_create_and_update_claim_images_and_preserve_url_attachments()
    {
        await using var factory = new InMemoryDatabaseFactory();
        await using var db = await factory.CreateSelfServiceDbContext();
        var first = Asset(PrivateAssetOwnerType.Event);
        var second = Asset(PrivateAssetOwnerType.Event);
        db.PrivateAssets.AddRange(first, second);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var context = Context();
        var authorization = new Mock<IAuthorizationService>();
        authorization.Setup(service => service.CanCreateEvent(It.IsAny<PortalUser>())).ReturnsAsync(true);
        authorization.Setup(service => service.CanUpdateEvent(It.IsAny<PortalUser>())).ReturnsAsync(true);
        using var services = TransactionalServiceFactory.Create(db);
        var service = services.GetRequiredService<IEventService>();
        var controller = new EventController(Resources(context, authorization.Object), service, authorization.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context },
        };

        var created = Assert.IsType<EventApiResource>(
            Assert
                .IsType<OkObjectResult>(
                    await controller.CreateEvent(
                        new EventCreateRequest
                        {
                            Title = "Event",
                            Description = "Body",
                            EventDate = DateTime.UtcNow,
                            Type = EventType.Demo,
                            AssetIds = [(Guid)first.Id],
                            Attachments =
                            [
                                new EventAttachmentCreateDto
                                {
                                    Url = "https://example.test",
                                    Type = EventAttachmentType.Image,
                                },
                            ],
                        }
                    )
                )
                .Value
        );
        Assert.Single(created.Attachments);
        Assert.Equal(
            (Guid)created.Id,
            (await db.PrivateAssets.AsNoTracking().SingleAsync(asset => asset.Id == first.Id)).OwnerId
        );

        db.ChangeTracker.Clear();
        Assert.IsType<OkObjectResult>(
            await controller.UpdateEvent(
                created.Id,
                new EventUpdateRequest { Title = "Updated", AssetIds = [(Guid)second.Id] }
            )
        );
        Assert.Equal(
            2,
            (await new PrivateAssetRepository(db).GetByOwner(PrivateAssetOwnerType.Event, created.Id)).Count
        );
        Assert.IsType<BadRequestObjectResult>(
            await controller.UpdateEvent(
                created.Id,
                new EventUpdateRequest { Title = "Not saved", AssetIds = [Guid.NewGuid()] }
            )
        );
        db.ChangeTracker.Clear();
        Assert.Equal("Updated", (await db.Events.SingleAsync()).Title);
        Assert.Single(await db.EventAttachments.ToListAsync());

        db.ChangeTracker.Clear();
        Assert.IsType<OkObjectResult>(
            await controller.UpdateEvent(
                created.Id,
                new EventUpdateRequest
                {
                    Attachments =
                    [
                        new EventAttachmentDto
                        {
                            Id = created.Attachments[0].Id,
                            Url = "https://updated.test",
                            Type = EventAttachmentType.Image,
                        },
                        new EventAttachmentDto { Url = "https://new.test", Type = EventAttachmentType.Document },
                    ],
                }
            )
        );
        db.ChangeTracker.Clear();
        var attachments = await db.EventAttachments.ToListAsync();
        Assert.Equal(2, attachments.Count);
        Assert.Contains(attachments, attachment => attachment.Url == "https://updated.test");
        var newAttachment = Assert.Single(attachments, attachment => attachment.Url == "https://new.test");

        db.ChangeTracker.Clear();
        Assert.IsType<OkObjectResult>(
            await controller.UpdateEvent(
                created.Id,
                new EventUpdateRequest
                {
                    Attachments =
                    [
                        new EventAttachmentDto
                        {
                            Id = newAttachment.Id,
                            Url = newAttachment.Url,
                            Type = newAttachment.AttachmentType,
                        },
                    ],
                }
            )
        );
        db.ChangeTracker.Clear();
        Assert.Equal(newAttachment.Id, (await db.EventAttachments.SingleAsync()).Id);
    }
}
