using FamilyTaskManager.Core.UserAggregate;

namespace FamilyTaskManager.Core.Specifications;

public class GetTelegramSessionByUserIdSpec(Guid userId) : Specification<TelegramSession>
{
  public void Configure() =>
    Query.Where(s => s.UserId == userId);
}
