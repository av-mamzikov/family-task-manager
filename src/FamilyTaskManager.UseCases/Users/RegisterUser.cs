namespace FamilyTaskManager.UseCases.Users;

public record RegisterUserCommand(long TelegramId, string Name) : ICommand<Result<Guid>>;

public class RegisterUserHandler(IRepository<User> userRepository) : ICommandHandler<RegisterUserCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
  {
    // Check if user already exists
    var existingUserSpec = new GetUserByTelegramIdSpec(command.TelegramId);
    var existingUser = await userRepository.FirstOrDefaultAsync(existingUserSpec, cancellationToken);

    if (existingUser != null)
    {
      // Update name if changed
      if (existingUser.Name != command.Name)
      {
        existingUser.UpdateName(command.Name);
        await userRepository.UpdateAsync(existingUser, cancellationToken);
      }
      return Result<Guid>.Success(existingUser.Id);
    }

    // Create new user
    var user = new User(command.TelegramId, command.Name);
    await userRepository.AddAsync(user, cancellationToken);

    return Result<Guid>.Success(user.Id);
  }
}
