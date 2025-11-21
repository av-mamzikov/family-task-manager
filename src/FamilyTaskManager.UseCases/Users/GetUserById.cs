namespace FamilyTaskManager.UseCases.Users;

public record UserDto(Guid Id, long TelegramId, string Name);

public record GetUserByIdQuery(Guid UserId) : IQuery<Result<UserDto?>>;

public class GetUserByIdHandler(IRepository<User> repository)
  : IQueryHandler<GetUserByIdQuery, Result<UserDto?>>
{
  public async ValueTask<Result<UserDto?>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
  {
    var user = await repository.GetByIdAsync(request.UserId, cancellationToken);
    
    if (user == null)
    {
      return Result<UserDto?>.Success(null);
    }

    return Result<UserDto?>.Success(new UserDto(user.Id, user.TelegramId, user.Name));
  }
}
