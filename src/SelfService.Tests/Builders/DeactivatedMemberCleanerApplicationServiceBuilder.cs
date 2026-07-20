using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Infrastructure.BackgroundJobs;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class DeactivatedMemberCleanerApplicationServiceBuilder
{
    private IMembershipRepository _membershipRepository;
    private IMemberRepository _memberRepository;
    private IMembershipApplicationRepository _membershipApplicationRepository;
    private IMissingMemberRepository _missingMemberRepository;
    private IRbacPermissionGrantRepository _rbacPermissionGrantRepository;
    private IRbacRoleGrantRepository _rbacRoleGrantRepository;
    private IRbacGroupMemberRepository _rbacGroupMemberRepository;
    private ILogger<DeactivatedMemberCleanerApplicationService> _logger; //make correct logger

    public DeactivatedMemberCleanerApplicationServiceBuilder()
    {
        _membershipRepository = Dummy.Of<IMembershipRepository>();
        _memberRepository = Dummy.Of<IMemberRepository>();
        _membershipApplicationRepository = Dummy.Of<IMembershipApplicationRepository>();
        _missingMemberRepository = Dummy.Of<IMissingMemberRepository>();

        // Configure RBAC repositories to return empty lists instead of null
        var permissionGrantMock = new Mock<IRbacPermissionGrantRepository>();
        permissionGrantMock
            .Setup(x => x.GetAllWithPredicate(It.IsAny<Func<RbacPermissionGrant, bool>>()))
            .ReturnsAsync(new List<RbacPermissionGrant>());
        _rbacPermissionGrantRepository = permissionGrantMock.Object;

        var roleGrantMock = new Mock<IRbacRoleGrantRepository>();
        roleGrantMock
            .Setup(x => x.GetByAssignedUsers(It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync(new List<RbacRoleGrant>());
        _rbacRoleGrantRepository = roleGrantMock.Object;

        var groupMemberMock = new Mock<IRbacGroupMemberRepository>();
        groupMemberMock
            .Setup(x => x.GetAllWithPredicate(It.IsAny<Func<RbacGroupMember, bool>>()))
            .ReturnsAsync(new List<RbacGroupMember>());
        _rbacGroupMemberRepository = groupMemberMock.Object;

        _logger = Dummy.Of<ILogger<DeactivatedMemberCleanerApplicationService>>();
    }

    public DeactivatedMemberCleanerApplicationServiceBuilder WithMembershipRepository(
        IMembershipRepository membershipRepository
    )
    {
        _membershipRepository = membershipRepository;
        return this;
    }

    public DeactivatedMemberCleanerApplicationServiceBuilder WithMemberRepository(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
        return this;
    }

    public DeactivatedMemberCleanerApplicationServiceBuilder WithMembershipApplicationRepository(
        IMembershipApplicationRepository membershipApplicationRepository
    )
    {
        _membershipApplicationRepository = membershipApplicationRepository;
        return this;
    }

    public DeactivatedMemberCleanerApplicationServiceBuilder WithMissingMemberRepository(
        IMissingMemberRepository missingMemberRepository
    )
    {
        _missingMemberRepository = missingMemberRepository;
        return this;
    }

    public DeactivatedMemberCleanerApplicationServiceBuilder WithRbacPermissionGrantRepository(
        IRbacPermissionGrantRepository rbacPermissionGrantRepository
    )
    {
        _rbacPermissionGrantRepository = rbacPermissionGrantRepository;
        return this;
    }

    public DeactivatedMemberCleanerApplicationServiceBuilder WithRbacRoleGrantRepository(
        IRbacRoleGrantRepository rbacRoleGrantRepository
    )
    {
        _rbacRoleGrantRepository = rbacRoleGrantRepository;
        return this;
    }

    public DeactivatedMemberCleanerApplicationServiceBuilder WithRbacGroupMemberRepository(
        IRbacGroupMemberRepository rbacGroupMemberRepository
    )
    {
        _rbacGroupMemberRepository = rbacGroupMemberRepository;
        return this;
    }

    public DeactivatedMemberCleanerApplicationService Build()
    {
        return new DeactivatedMemberCleanerApplicationService(
            logger: _logger,
            membershipRepository: _membershipRepository,
            memberRepository: _memberRepository,
            membershipApplicationRepository: _membershipApplicationRepository,
            missingMemberRepository: _missingMemberRepository,
            rbacPermissionGrantRepository: _rbacPermissionGrantRepository,
            rbacRoleGrantRepository: _rbacRoleGrantRepository,
            rbacGroupMemberRepository: _rbacGroupMemberRepository
        );
    }

    public static implicit operator DeactivatedMemberCleanerApplicationService(
        DeactivatedMemberCleanerApplicationServiceBuilder builder
    ) => builder.Build();
}
