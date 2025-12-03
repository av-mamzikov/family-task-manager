namespace FamilyTaskManager.UseCases.Families;

public record JoinByInviteCodeCommand(Guid UserId, string Code) : ICommand<Result<Guid>>;

public class JoinByInviteCodeHandler(
  IAppRepository<Family> familyAppRepository,
  IAppRepository<User> userAppRepository,
  IAppRepository<Invitation> invitationAppRepository) : ICommandHandler<JoinByInviteCodeCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(JoinByInviteCodeCommand command, CancellationToken cancellationToken)
  {
    // Verify user exists
    var user = await userAppRepository.GetByIdAsync(command.UserId, cancellationToken);
    if (user == null)
    {
      return Result<Guid>.NotFound("User not found");
    }

    // Find invitation by code
    var invitationSpec = new GetInvitationByCodeSpec(command.Code);
    var invitation = await invitationAppRepository.FirstOrDefaultAsync(invitationSpec, cancellationToken);

    if (invitation == null)
    {
      return Result<Guid>.NotFound("Invitation not found");
    }

    // Validate invitation
    if (!invitation.IsValid())
    {
      if (invitation.IsExpired())
      {
        return Result<Guid>.Error("Invitation has expired");
      }

      return Result<Guid>.Error("Invitation is not active");
    }

    // Get family with members
    var familySpec = new GetFamilyWithMembersSpec(invitation.FamilyId);
    var family = await familyAppRepository.FirstOrDefaultAsync(familySpec, cancellationToken);

    if (family == null)
    {
      return Result<Guid>.NotFound("Family not found");
    }

    // Check if user is already a member
    if (family.Members.Any(m => m.UserId == command.UserId && m.IsActive))
    {
      return Result<Guid>.Error("User is already a member of this family");
    }

    // Add member with the role specified in the invitation (user entity needed for event)
    var member = family.AddMember(user, invitation.Role);

    // Deactivate the invitation (one-time use)
    invitation.Deactivate();

    // Update both entities
    await familyAppRepository.UpdateAsync(family, cancellationToken);
    await invitationAppRepository.UpdateAsync(invitation, cancellationToken);
    await familyAppRepository.SaveChangesAsync(cancellationToken);

    return Result<Guid>.Success(member.Id);
  }
}
