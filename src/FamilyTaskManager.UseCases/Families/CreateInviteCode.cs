namespace FamilyTaskManager.UseCases.Families;

public record CreateInviteCodeCommand(Guid FamilyId, FamilyRole Role, Guid CreatedBy, int ExpirationDays = 7) 
  : ICommand<Result<string>>;

public class CreateInviteCodeHandler(
  IRepository<Family> familyRepository,
  IRepository<Invitation> invitationRepository) : ICommandHandler<CreateInviteCodeCommand, Result<string>>
{
  public async ValueTask<Result<string>> Handle(CreateInviteCodeCommand command, CancellationToken cancellationToken)
  {
    // Verify family exists
    var family = await familyRepository.GetByIdAsync(command.FamilyId, cancellationToken);
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
    
    await invitationRepository.AddAsync(invitation, cancellationToken);

    return Result<string>.Success(invitation.Code);
  }
}
