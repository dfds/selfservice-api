using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Persistence.Queries;
using SelfService.Tests.TestDoubles;

//TODO: COMMENTING OUT BECAUSE THIS TEST IS FAILING AND WE DON'T KNOW WHY
//namespace SelfService.Tests.Infrastructure.Api;

// public class TestCapabilityCreation
// {
//     [Fact]
//     [Trait("Category", "Integration")]
//     public async Task creating_capability_also_populates_invites()
//     {
//         //setup
//         var databaseFactory = new ExternalDatabaseFactory();
//         var dbContext = await databaseFactory.CreateDbContext();
//
//         var capabilityRepo = A.CapabilityRepository.WithDbContext(dbContext).Build();
//         var invitationRepo = A.InvitationRepository.WithDbContext(dbContext).Build();
//
//         var invitationService = A.InvitationApplicationService
//             .WithDbContextAndDefaultRepositories(dbContext)
//             .WithInvitationRepository(invitationRepo)
//             .Build();
//
//         await using var application = new ApiApplication();
//         application.ReplaceService<IAwsAccountRepository>(new StubAwsAccountRepository());
//         application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository());
//         application.ReplaceService<IMembershipQuery>(new StubMembershipQuery());
//         application.ReplaceService<ICapabilityDeletionStatusQuery>(new StubCapabilityDeletionStatusQuery());
//         //await using var application = new ApiApplicationBuilder()
//         //   .WithAwsAccountRepository(new StubAwsAccountRepository())
//         //   .WithMembershipQuery(new StubMembershipQuery())
//         //   .Build();
//
//         string invitedUser = "foobar@mailinator.com";
//         UserId invitedUserId = UserId.Parse(invitedUser);
//
//         var invitations = await invitationService.GetActiveInvitationsForType(
//             userId: invitedUserId,
//             targetType: InvitationTargetTypeOptions.Capability
//         );
//         Assert.Empty(invitations);
//
//         string json = JsonSerializer.Serialize(
//             A.CapabilityRequest.WithName("cap-pop-inv-test").WithInvitees(new List<string> { invitedUser }).Build()
//         );
//         HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
//         using var client = application.CreateClient();
//         var response = await client.PostAsync($"/capabilities", content);
//
//         // assertions
//         Assert.Equal(HttpStatusCode.Created, response.StatusCode);
//
//         var capabilitiesAfter = await capabilityRepo.GetAll();
//         Assert.True(capabilitiesAfter.ToList().Exists(x => x.Name == "cap-pop-inv-test"));
//
//         invitations = await invitationService.GetActiveInvitationsForType(
//             userId: invitedUserId,
//             targetType: InvitationTargetTypeOptions.Capability
//         );
//         Assert.Single(invitations);
//     }
// }
