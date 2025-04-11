using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Application;

public class InvitationApplicationService : IInvitationApplicationService
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IMembershipApplicationRepository _membershipApplicationRepository;
    private readonly ILogger<InvitationApplicationService> _logger;

    public InvitationApplicationService(
        IInvitationRepository invitationRepository,
        ICapabilityRepository capabilityRepository,
        IMembershipRepository membershipRepository,
        IMembershipApplicationRepository membershipApplicationRepository,
        ILogger<InvitationApplicationService> logger
    )
    {
        _invitationRepository = invitationRepository;
        _capabilityRepository = capabilityRepository;
        _membershipRepository = membershipRepository;
        _membershipApplicationRepository = membershipApplicationRepository;
        _logger = logger;
    }

    public async Task<List<Invitation>> GetActiveInvitations(UserId userId)
    {
        var invitations = await _invitationRepository.GetAllWithPredicate(x =>
            x.Invitee == userId && x.Status == InvitationStatusOptions.Active
        );

        return invitations;
    }

    public async Task<List<Invitation>> GetActiveInvitationsForType(
        UserId userId,
        InvitationTargetTypeOptions targetType
    )
    {
        var invitations = await _invitationRepository.GetAllWithPredicate(x =>
            x.Invitee == userId && x.Status == InvitationStatusOptions.Active && x.TargetType == targetType
        );

        return invitations;
    }

    public async Task<Invitation> GetInvitation(InvitationId invitationId)
    {
        var invitation = await _invitationRepository.FindById(invitationId);
        if (invitation == null)
        {
            throw new EntityNotFoundException("Invitation does not exist");
        }

        return invitation;
    }

    [TransactionalBoundary]
    public async Task<Invitation> DeclineInvitation(InvitationId invitationId)
    {
        var invitation = await _invitationRepository.FindById(invitationId);
        if (invitation == null)
        {
            throw new EntityNotFoundException("Invitation does not exist");
        }

        invitation.Decline();

        // cancel all similar invitations
        var similarInvitations = await _invitationRepository.GetOtherActiveInvitationsForSameTarget(
            invitation.Invitee,
            invitation.TargetId,
            invitation.Id
        );
        foreach (var i in similarInvitations)
        {
            i.Cancel();
        }

        // cancel all similar applications
        var similarApplications = await _membershipApplicationRepository.GetAllForUserAndCapability(
            userId: invitation.Invitee,
            capabilityId: invitation.TargetId
        );
        foreach (var a in similarApplications)
        {
            a.Cancel();
        }

        return invitation;
    }

    [TransactionalBoundary]
    public async Task<Invitation> AcceptInvitation(InvitationId invitationId)
    {
        var invitation = await _invitationRepository.FindById(invitationId);
        if (invitation == null)
        {
            throw new EntityNotFoundException("Invitation does not exist");
        }

        if (invitation.TargetType == InvitationTargetTypeOptions.Capability)
        {
            if (!CapabilityId.TryParse(invitation.TargetId, out var capabilityId))
            {
                throw new EntityNotFoundException("Invalid capability Id");
            }

            if (!await _capabilityRepository.Exists(capabilityId))
            {
                throw new EntityNotFoundException("Capability does not exist");
            }

            if (await _membershipRepository.IsAlreadyMember(capabilityId, invitation.Invitee))
            {
                invitation.Cancel();
            }
            else
            {
                invitation.Accept();

                var membership = new Membership(
                    id: MembershipId.New(),
                    capabilityId: capabilityId,
                    userId: invitation.Invitee,
                    createdAt: DateTime.UtcNow
                );
                await _membershipRepository.Add(membership);
            }

            // cancel all similar invitations
            var similarInvitations = await _invitationRepository.GetOtherActiveInvitationsForSameTarget(
                invitation.Invitee,
                invitation.TargetId,
                invitation.Id
            );
            foreach (var i in similarInvitations)
            {
                i.Cancel();
            }

            // cancel all similar applications
            var similarApplications = await _membershipApplicationRepository.GetAllForUserAndCapability(
                userId: invitation.Invitee,
                capabilityId: invitation.TargetId
            );
            foreach (var a in similarApplications)
            {
                a.Cancel();
            }

            return invitation;
        }

        throw new NotSupportedException("Only capabilities are supported for invitations");
    }

    [TransactionalBoundary]
    public async Task<Invitation> CreateInvitation(
        UserId invitee,
        string description,
        string targetId,
        InvitationTargetTypeOptions targetType,
        UserId createdBy
    )
    {
        var invitation = new Invitation(
            id: InvitationId.New(),
            invitee: invitee,
            description: description,
            targetId: targetId,
            targetType: targetType,
            status: InvitationStatusOptions.Active,
            createdBy: createdBy,
            createdAt: DateTime.UtcNow,
            modifiedAt: DateTime.UtcNow
        );

        await _invitationRepository.Add(invitation);

        return invitation;
    }

    [TransactionalBoundary]
    public async Task<List<Invitation>> CreateCapabilityInvitations(
        List<string> invitees,
        UserId inviter,
        Capability capability
    )
    {
        var description = $"\"{inviter}\" has invited you to join capability \"{capability.Name}\"";
        var invitations = new List<Invitation>();
        var dedupedInvitees = invitees.Distinct().ToList();
        foreach (var invitee in dedupedInvitees)
        {
            if (!UserId.TryParse(invitee, out var inviteeId))
            {
                _logger.LogWarning("Unable to parse invitee \"{Invitee}\" as a valid user id", invitee);
                continue;
            }

            if (await _membershipRepository.IsAlreadyMember(capability.Id, inviteeId))
            {
                _logger.LogWarning(
                    "User \"{Invitee}\" is already member of \"{Capability}\"",
                    inviteeId,
                    capability.Id
                );
                continue;
            }
            var existingInvitations = await _invitationRepository.GetActiveInvitations(inviteeId, capability.Id);
            if (existingInvitations.Any())
            {
                _logger.LogWarning(
                    "User \"{Invitee}\" is already invited to \"{Capability}\"",
                    inviteeId,
                    capability.Id
                );
                continue;
            }

            var invitation = await CreateInvitation(
                invitee: inviteeId,
                description: description,
                targetId: capability.Id,
                targetType: InvitationTargetTypeOptions.Capability,
                createdBy: inviter
            );
            invitations.Add(invitation);
        }
        return invitations;
    }
}
