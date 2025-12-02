namespace FamilyTaskManager.UseCases.Families;

public record JoinFamilyCommand(Guid UserId, Guid FamilyId, FamilyRole Role) : ICommand<Result<Guid>>;

public class JoinFamilyHandler(
  IRepository<Family> familyRepository,
  IRepository<User> userRepository) : ICommandHandler<JoinFamilyCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(JoinFamilyCommand command, CancellationToken cancellationToken)
  {
    // Verify user exists
    var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
    if (user == null)
    {
      return Result<Guid>.NotFound("User not found");
    }

    // Get family
    var spec = new GetFamilyWithMembersSpec(command.FamilyId);
    var family = await familyRepository.FirstOrDefaultAsync(spec, cancellationToken);
    if (family == null)
    {
      return Result<Guid>.NotFound("Family not found");
    }

    // Check if user is already a member
    if (family.Members.Any(m => m.UserId == command.UserId && m.IsActive))
    {
      return Result<Guid>.Error("User is already a member of this family");
    }

    // Add member (registers MemberAddedEvent with user.Name)
    var member = family.AddMember(user, command.Role);

    // Update family (domain events will be dispatched automatically)
    await familyRepository.UpdateAsync(family, cancellationToken);
    await familyRepository.SaveChangesAsync(cancellationToken);

    return Result<Guid>.Success(member.Id);
  }
}
