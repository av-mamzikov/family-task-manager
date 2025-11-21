using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.UseCases.Users;
using FamilyTaskManager.UseCases.Users.Specifications;

namespace FamilyTaskManager.UnitTests.UseCases.Users;

public class RegisterUserHandlerTests
{
  private readonly IRepository<User> _userRepository;
  private readonly RegisterUserHandler _handler;

  public RegisterUserHandlerTests()
  {
    _userRepository = Substitute.For<IRepository<User>>();
    _handler = new RegisterUserHandler(_userRepository);
  }

  [Fact]
  public async Task Handle_NewUser_CreatesUserAndReturnsId()
  {
    // Arrange
    var command = new RegisterUserCommand(TelegramId: 123456789, Name: "John Doe");
    _userRepository.FirstOrDefaultAsync(Arg.Any<GetUserByTelegramIdSpec>(), Arg.Any<CancellationToken>())
      .Returns((User?)null);
    
    // Capture the added user to get its ID
    User? capturedUser = null;
    await _userRepository.AddAsync(Arg.Do<User>(u => capturedUser = u), Arg.Any<CancellationToken>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedUser.ShouldNotBeNull();
    result.Value.ShouldBe(capturedUser.Id);
    capturedUser.TelegramId.ShouldBe(command.TelegramId);
    capturedUser.Name.ShouldBe(command.Name);
  }

  [Fact]
  public async Task Handle_ExistingUser_ReturnsExistingUserId()
  {
    // Arrange
    var existingUser = new User(123456789, "John Doe");
    var command = new RegisterUserCommand(TelegramId: 123456789, Name: "John Doe");
    
    _userRepository.FirstOrDefaultAsync(Arg.Any<GetUserByTelegramIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(existingUser);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBe(existingUser.Id);
    await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ExistingUserWithDifferentName_UpdatesNameAndReturnsId()
  {
    // Arrange
    var existingUser = new User(123456789, "Old Name");
    var command = new RegisterUserCommand(TelegramId: 123456789, Name: "New Name");
    
    _userRepository.FirstOrDefaultAsync(Arg.Any<GetUserByTelegramIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(existingUser);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBe(existingUser.Id);
    existingUser.Name.ShouldBe("New Name");
    await _userRepository.Received(1).UpdateAsync(existingUser, Arg.Any<CancellationToken>());
  }
}
