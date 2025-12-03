using Ardalis.Result;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.UseCases.Families;

namespace FamilyTaskManager.UnitTests.UseCases.Families;

public class CreateFamilyHandlerTests
{
  private readonly IAppRepository<Family> _familyAppRepository;
  private readonly CreateFamilyHandler _handler;
  private readonly ITimeZoneService _timeZoneService;
  private readonly IAppRepository<User> _userAppRepository;

  public CreateFamilyHandlerTests()
  {
    _familyAppRepository = Substitute.For<IAppRepository<Family>>();
    _userAppRepository = Substitute.For<IAppRepository<User>>();
    _timeZoneService = Substitute.For<ITimeZoneService>();
    _handler = new(_familyAppRepository, _userAppRepository, _timeZoneService);
  }

  [Fact]
  public async Task Handle_ValidCommand_CreatesFamilyWithAdminMember()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var user = new User(123456789, "John Doe");
    var command = new CreateFamilyCommand(userId, "Smith Family", "Europe/Moscow");

    _userAppRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);
    _timeZoneService.IsValidTimeZone("Europe/Moscow").Returns(true);

    Family? capturedFamily = null;
    await _familyAppRepository.AddAsync(Arg.Do<Family>(f => capturedFamily = f), Arg.Any<CancellationToken>());

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
    capturedFamily.Members.First().UserId.ShouldBe(user.Id);
    capturedFamily.Members.First().Role.ShouldBe(FamilyRole.Admin);
  }

  [Fact]
  public async Task Handle_NonExistentUser_ReturnsNotFound()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var command = new CreateFamilyCommand(userId, "Smith Family", "UTC");

    _userAppRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns((User?)null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.NotFound);
    await _familyAppRepository.DidNotReceive().AddAsync(Arg.Any<Family>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_DefaultTimezone_UsesUTC()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var user = new User(123456789, "John Doe");
    var command = new CreateFamilyCommand(userId, "Smith Family", "UTC");

    _userAppRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);
    _timeZoneService.IsValidTimeZone("UTC").Returns(true);

    Family? capturedFamily = null;
    await _familyAppRepository.AddAsync(Arg.Do<Family>(f => capturedFamily = f), Arg.Any<CancellationToken>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedFamily.ShouldNotBeNull();
    capturedFamily.Timezone.ShouldBe("UTC");
  }
}
