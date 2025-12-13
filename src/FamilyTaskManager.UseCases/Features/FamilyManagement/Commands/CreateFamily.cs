namespace FamilyTaskManager.UseCases.Features.FamilyManagement.Commands;

public record CreateFamilyCommand(Guid UserId, string Name, string Timezone, bool LeaderboardEnabled = true)
  : ICommand<Result<Guid>>;

public class CreateFamilyHandler(
  IAppRepository<Family> familyAppRepository,
  IAppRepository<User> userAppRepository,
  ITimeZoneService timeZoneService) : ICommandHandler<CreateFamilyCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(CreateFamilyCommand command, CancellationToken cancellationToken)
  {
    // Verify user exists
    var user = await userAppRepository.GetByIdAsync(command.UserId, cancellationToken);
    if (user == null) return Result<Guid>.NotFound("Пользователь не найден");

    // Validate timezone
    if (!timeZoneService.IsValidTimeZone(command.Timezone))
      return Result<Guid>.Invalid(new ValidationError($"Неверный часовой пояс: {command.Timezone}"));

    // Create family
    var family = new Family(command.Name, command.Timezone, command.LeaderboardEnabled);

    // Add creator as Admin (user entity needed for MemberAddedEvent)
    family.AddMember(user, FamilyRole.Admin);

    await familyAppRepository.AddAsync(family, cancellationToken);
    await familyAppRepository.SaveChangesAsync(cancellationToken);

    return Result<Guid>.Success(family.Id);
  }
}
