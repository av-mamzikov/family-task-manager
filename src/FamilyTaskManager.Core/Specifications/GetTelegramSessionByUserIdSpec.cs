using FamilyTaskManager.Core.UserAggregate;

namespace FamilyTaskManager.Core.Specifications;

public class GetTelegramSessionByUserIdSpec : Specification<TelegramSession>
{
  public GetTelegramSessionByUserIdSpec(Guid userId)
  {
    Query.Where(s => s.UserId == userId);
  }
}
