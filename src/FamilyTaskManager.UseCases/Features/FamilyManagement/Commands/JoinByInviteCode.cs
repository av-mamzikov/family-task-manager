using FamilyTaskManager.Core.FamilyAggregate.Specifications;

namespace FamilyTaskManager.UseCases.Features.FamilyManagement.Commands;

public record JoinResult(Family Family, FamilyMember Member);

public record JoinByInviteCodeCommand(Guid UserId, string Code) : ICommand<Result<JoinResult>>;

public class JoinByInviteCodeHandler(
  IAppRepository<Family> familyAppRepository,
  IAppRepository<User> userAppRepository,
  IAppRepository<Invitation> invitationAppRepository) : ICommandHandler<JoinByInviteCodeCommand, Result<JoinResult>>
{
  public async ValueTask<Result<JoinResult>> Handle(JoinByInviteCodeCommand command,
    CancellationToken cancellationToken)
  {
    // Verify user exists
    var user = await userAppRepository.GetByIdAsync(command.UserId, cancellationToken);
    if (user == null) return Result<JoinResult>.NotFound("User not found");

    // Find invitation by code
    var invitationSpec = new GetInvitationByCodeSpec(command.Code);
    var invitation = await invitationAppRepository.FirstOrDefaultAsync(invitationSpec, cancellationToken);

    if (invitation == null) return Result<JoinResult>.NotFound("Invitation not found");

    // Validate invitation
    if (!invitation.IsValid())
    {
      if (invitation.IsExpired()) return Result<JoinResult>.Error("Invitation has expired");

      return Result<JoinResult>.Error("Invitation is not active");
    }

    // Get family with members
    var familySpec = new GetFamilyWithMembersSpec(invitation.FamilyId);
    var family = await familyAppRepository.FirstOrDefaultAsync(familySpec, cancellationToken);

    if (family == null) return Result<JoinResult>.NotFound("Family not found");

    // Check if user is already a member
    if (family.Members.Any(m => m.UserId == command.UserId && m.IsActive))
      return Result<JoinResult>.Error("User is already a member of this family");

    // Add member with the role specified in the invitation (user entity needed for event)
    var member = family.AddMember(user, invitation.Role);

    // Deactivate the invitation (one-time use)
    invitation.Deactivate();

    // Update both entities
    await familyAppRepository.UpdateAsync(family, cancellationToken);
    await invitationAppRepository.UpdateAsync(invitation, cancellationToken);
    await familyAppRepository.SaveChangesAsync(cancellationToken);

    return Result<JoinResult>.Success(new(family, member));
  }
}
