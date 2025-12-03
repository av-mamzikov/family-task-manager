namespace FamilyTaskManager.UseCases.Families;

public record CreateInviteCodeCommand(Guid FamilyId, FamilyRole Role, Guid CreatedBy, int ExpirationDays = 7)
  : ICommand<Result<string>>;

public class CreateInviteCodeHandler(
  IAppRepository<Family> familyAppRepository,
  IAppRepository<Invitation> invitationAppRepository) : ICommandHandler<CreateInviteCodeCommand, Result<string>>
{
  public async ValueTask<Result<string>> Handle(CreateInviteCodeCommand command, CancellationToken cancellationToken)
  {
    // Load family with members to validate creator membership and role
    var familySpec = new GetFamilyWithMembersSpec(command.FamilyId);
    var family = await familyAppRepository.FirstOrDefaultAsync(familySpec, cancellationToken);
    if (family == null)
    {
      return Result<string>.NotFound("Family not found");
    }

    // Verify creator is a member of the family with appropriate permissions
    var creatorMember = family.Members.FirstOrDefault(m => m.UserId == command.CreatedBy && m.IsActive);
    if (creatorMember == null)
    {
      return Result<string>.Forbidden("User is not a member of this family");
    }

    // Only Admin can create invitations
    if (creatorMember.Role != FamilyRole.Admin)
    {
      return Result<string>.Forbidden("Only admins can create invite codes");
    }

    // Create invitation
    var invitation = new Invitation(command.FamilyId, command.Role, command.CreatedBy, command.ExpirationDays);

    await invitationAppRepository.AddAsync(invitation, cancellationToken);

    return Result<string>.Success(invitation.Code);
  }
}
