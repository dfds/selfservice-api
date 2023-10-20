using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Application;

public class InvitationApplicationService : IInvitationApplicationService
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly ILogger<InvitationApplicationService> _logger;

    public InvitationApplicationService(
        IInvitationRepository invitationRepository,
        ICapabilityRepository capabilityRepository,
        IMembershipRepository membershipRepository,
        ILogger<InvitationApplicationService> logger
    )
    {
        _invitationRepository = invitationRepository;
        _capabilityRepository = capabilityRepository;
        _membershipRepository = membershipRepository;
        _logger = logger;
    }

    public async Task<List<Invitation>> GetActiveInvitations(UserId userId)
    {
        var invitations = await _invitationRepository.GetAllWithPredicate(
            x => x.Invitee == userId && x.Status == InvitationStatusOptions.Active
        );

        return invitations;
    }

    public async Task<List<Invitation>> GetActiveInvitationsForType(
        UserId userId,
        InvitationTargetTypeOptions targetType
    )
    {
        var invitations = await _invitationRepository.GetAllWithPredicate(
            x => x.Invitee == userId && x.Status == InvitationStatusOptions.Active && x.TargetType == targetType
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
            CapabilityId.TryParse(invitation.TargetId, out var capabilityId);
            if (capabilityId == null)
            {
                throw new EntityNotFoundException("Invalid capability Id");
            }

            var capability = await _capabilityRepository.FindBy(capabilityId);
            if (capability == null)
            {
                throw new EntityNotFoundException("Capability does not exist");
            }

            invitation.Accept();

            var membership = new Membership(
                id: MembershipId.New(),
                capabilityId: capabilityId,
                userId: invitation.Invitee,
                createdAt: DateTime.UtcNow
            );
            await _membershipRepository.Add(membership);

            return invitation;
        }

        // Currently we only have memberships for capabilities.
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

    public async Task<List<Invitation>> CreateCapabilityInvitations(
        List<string> invitees,
        UserId inviter,
        Capability capability
    )
    {
        var description = $"\"{inviter}\" has invited you to join capability \"{capability.Description}\"";
        var invitations = new List<Invitation>();
        foreach (var invitee in invitees)
        {
            if (UserId.TryParse(invitee, out var inviteeId))
            {
                var invitation = await CreateInvitation(
                    invitee: inviteeId,
                    description: description,
                    targetId: capability.Id.ToString(),
                    targetType: InvitationTargetTypeOptions.Capability,
                    createdBy: inviter
                );
                invitations.Add(invitation);
            }
            else
            {
                _logger.LogWarning($"Unable to parse invitee \"{invitee}\" as a valid user id", invitee);
            }
            ;
        }
        return invitations;
    }
}
