namespace FamilyTaskManager.UseCases.Users.Specifications;

public class GetUserByTelegramIdSpec : Specification<User>
{
  public GetUserByTelegramIdSpec(long telegramId)
  {
    Query.Where(u => u.TelegramId == telegramId);
  }
}
