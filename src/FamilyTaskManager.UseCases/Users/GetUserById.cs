using FamilyTaskManager.UseCases.Contracts;

namespace FamilyTaskManager.UseCases.Users;

public record GetUserByIdQuery(Guid UserId) : IQuery<Result<UserDto?>>;

public class GetUserByIdHandler(IAppRepository<User> appRepository)
  : IQueryHandler<GetUserByIdQuery, Result<UserDto?>>
{
  public async ValueTask<Result<UserDto?>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
  {
    var user = await appRepository.GetByIdAsync(request.UserId, cancellationToken);

    if (user == null) return Result<UserDto?>.Success(null);

    return Result<UserDto?>.Success(new(user.Id, user.TelegramId, user.Name));
  }
}
