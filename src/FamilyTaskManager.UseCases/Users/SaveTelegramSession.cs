namespace FamilyTaskManager.UseCases.Users;

public record SaveTelegramSessionCommand(
  long TelegramId,
  Guid? CurrentFamilyId,
  int ConversationState,
  string? SessionDataJson) : ICommand<Result>;

public class SaveTelegramSessionHandler(
  IAppRepository<User> userAppRepository,
  IAppRepository<TelegramSession> sessionAppRepository)
  : ICommandHandler<SaveTelegramSessionCommand, Result>
{
  public async ValueTask<Result> Handle(
    SaveTelegramSessionCommand command,
    CancellationToken cancellationToken)
  {
    var user = await userAppRepository.FirstOrDefaultAsync(
      new GetUserByTelegramIdSpec(command.TelegramId),
      cancellationToken);

    if (user == null)
      return Result.NotFound("User not found");

    var dbSession = await sessionAppRepository.GetOrCreateAndSaveAsync(
      new GetTelegramSessionByUserIdSpec(user.Id),
      () => new TelegramSession(user.Id),
      cancellationToken);

    dbSession.UpdateSession(command.CurrentFamilyId, command.ConversationState, command.SessionDataJson);
    await sessionAppRepository.UpdateAsync(dbSession, cancellationToken);
    await sessionAppRepository.SaveChangesAsync(cancellationToken);

    return Result.Success();
  }
}
