using FamilyTaskManager.Core.FamilyAggregate.Specifications;

namespace FamilyTaskManager.UseCases.Features.FamilyManagement.Commands;

public record JoinFamilyCommand(Guid UserId, Guid FamilyId, FamilyRole Role) : ICommand<Result<Guid>>;

public class JoinFamilyHandler(
  IAppRepository<Family> familyAppRepository,
  IAppRepository<User> userAppRepository) : ICommandHandler<JoinFamilyCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(JoinFamilyCommand command, CancellationToken cancellationToken)
  {
    // Verify user exists
    var user = await userAppRepository.GetByIdAsync(command.UserId, cancellationToken);
    if (user == null) return Result<Guid>.NotFound("User not found");

    // Get family
    var spec = new GetFamilyWithMembersSpec(command.FamilyId);
    var family = await familyAppRepository.FirstOrDefaultAsync(spec, cancellationToken);
    if (family == null) return Result<Guid>.NotFound("Family not found");

    // Check if user is already a member
    if (family.Members.Any(m => m.UserId == command.UserId && m.IsActive))
      return Result<Guid>.Error("User is already a member of this family");

    // Add member (registers MemberAddedEvent with user.Name)
    var member = family.AddMember(user, command.Role);

    // Update family (domain events will be dispatched automatically)
    await familyAppRepository.UpdateAsync(family, cancellationToken);
    await familyAppRepository.SaveChangesAsync(cancellationToken);

    return Result<Guid>.Success(member.Id);
  }
}
