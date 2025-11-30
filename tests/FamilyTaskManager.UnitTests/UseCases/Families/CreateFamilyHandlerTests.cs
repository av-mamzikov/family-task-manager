using Ardalis.Result;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.UseCases.Families;

namespace FamilyTaskManager.UnitTests.UseCases.Families;

public class CreateFamilyHandlerTests
{
  private readonly IRepository<Family> _familyRepository;
  private readonly CreateFamilyHandler _handler;
  private readonly ITimeZoneService _timeZoneService;
  private readonly IRepository<User> _userRepository;

  public CreateFamilyHandlerTests()
  {
    _familyRepository = Substitute.For<IRepository<Family>>();
    _userRepository = Substitute.For<IRepository<User>>();
    _timeZoneService = Substitute.For<ITimeZoneService>();
    _handler = new CreateFamilyHandler(_familyRepository, _userRepository, _timeZoneService);
  }

  [Fact]
  public async Task Handle_ValidCommand_CreatesFamilyWithAdminMember()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var user = new User(123456789, "John Doe");
    var command = new CreateFamilyCommand(userId, "Smith Family", "Europe/Moscow");

    _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);
    _timeZoneService.IsValidTimeZone("Europe/Moscow").Returns(true);

    Family? capturedFamily = null;
    await _familyRepository.AddAsync(Arg.Do<Family>(f => capturedFamily = f), Arg.Any<CancellationToken>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedFamily.ShouldNotBeNull();
    result.Value.ShouldBe(capturedFamily.Id);
    capturedFamily.Name.ShouldBe("Smith Family");
    capturedFamily.Timezone.ShouldBe("Europe/Moscow");
    capturedFamily.LeaderboardEnabled.ShouldBeTrue();
    capturedFamily.Members.Count.ShouldBe(1);
    capturedFamily.Members.First().UserId.ShouldBe(userId);
    capturedFamily.Members.First().Role.ShouldBe(FamilyRole.Admin);
  }

  [Fact]
  public async Task Handle_NonExistentUser_ReturnsNotFound()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var command = new CreateFamilyCommand(userId, "Smith Family", "UTC");

    _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns((User?)null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.NotFound);
    await _familyRepository.DidNotReceive().AddAsync(Arg.Any<Family>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_DefaultTimezone_UsesUTC()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var user = new User(123456789, "John Doe");
    var command = new CreateFamilyCommand(userId, "Smith Family", "UTC");

    _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);
    _timeZoneService.IsValidTimeZone("UTC").Returns(true);

    Family? capturedFamily = null;
    await _familyRepository.AddAsync(Arg.Do<Family>(f => capturedFamily = f), Arg.Any<CancellationToken>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedFamily.ShouldNotBeNull();
    capturedFamily.Timezone.ShouldBe("UTC");
  }
}
