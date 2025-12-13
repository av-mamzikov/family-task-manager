using FamilyTaskManager.Core.UserAggregate.Specifications;

namespace FamilyTaskManager.UseCases.Features.UserManagement.Commads;

public record GetOrCreateTelegramSessionCommand(long TelegramId, string UserName) : ICommand<Result<TelegramSession>>;

public class GetOrCreateTelegramSessionHandler(
  IAppRepository<User> userAppRepository,
  IAppRepository<TelegramSession> sessionAppRepository)
  : ICommandHandler<GetOrCreateTelegramSessionCommand, Result<TelegramSession>>
{
  public async ValueTask<Result<TelegramSession>> Handle(
    GetOrCreateTelegramSessionCommand command,
    CancellationToken cancellationToken)
  {
    var user = await userAppRepository.GetOrCreateAndSaveAsync(
      new GetUserByTelegramIdSpec(command.TelegramId),
      () => new(command.TelegramId, command.UserName),
      cancellationToken);
    if (user.Name != command.UserName)
    {
      user.UpdateName(command.UserName);
      await userAppRepository.SaveChangesAsync(cancellationToken);
    }

    var session = await sessionAppRepository.GetOrCreateAndSaveAsync(
      new GetTelegramSessionByUserIdSpec(user.Id), () => new(user.Id), cancellationToken);

    return Result<TelegramSession>.Success(session);
  }
}
