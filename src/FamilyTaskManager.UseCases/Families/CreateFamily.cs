using FamilyTaskManager.Core.Interfaces;

namespace FamilyTaskManager.UseCases.Families;

public record CreateFamilyCommand(Guid UserId, string Name, string Timezone = "UTC", bool LeaderboardEnabled = true) 
  : ICommand<Result<Guid>>;

public class CreateFamilyHandler(
  IRepository<Family> familyRepository,
  IRepository<User> userRepository,
  ITimeZoneService timeZoneService) : ICommandHandler<CreateFamilyCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(CreateFamilyCommand command, CancellationToken cancellationToken)
  {
    // Verify user exists
    var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
    if (user == null)
    {
      return Result<Guid>.NotFound("User not found");
    }

    // Validate timezone
    if (!timeZoneService.IsValidTimeZone(command.Timezone))
    {
      return Result<Guid>.Invalid(new ValidationError($"Invalid timezone: {command.Timezone}"));
    }

    // Create family
    var family = new Family(command.Name, command.Timezone, command.LeaderboardEnabled);
    
    // Add creator as Admin
    family.AddMember(command.UserId, FamilyRole.Admin);
    
    await familyRepository.AddAsync(family, cancellationToken);

    return Result<Guid>.Success(family.Id);
  }
}
