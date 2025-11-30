using Ardalis.Result;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.UseCases.Users;
using Telegram.Bot.Types;

namespace FamilyTaskManager.Host.Modules.Bot.Services;

public interface IUserRegistrationService
{
  Task<Result<Guid>> GetOrRegisterUserAsync(User user, CancellationToken cancellationToken);
}

public class UserRegistrationService(IMediator mediator) : IUserRegistrationService
{
  public async Task<Result<Guid>> GetOrRegisterUserAsync(User user, CancellationToken cancellationToken)
  {
    var displayName = user.GetDisplayName();
    var registerCommand = new RegisterUserCommand(user.Id, displayName);
    return await mediator.Send(registerCommand, cancellationToken);
  }
}
