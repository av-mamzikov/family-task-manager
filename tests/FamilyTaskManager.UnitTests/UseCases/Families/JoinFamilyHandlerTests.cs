using Ardalis.Result;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.UnitTests.Helpers;
using FamilyTaskManager.UseCases.Families;
using FamilyTaskManager.UseCases.Families.Specifications;

namespace FamilyTaskManager.UnitTests.UseCases.Families;

public class JoinFamilyHandlerTests
{
  private readonly IAppRepository<Family> _familyAppRepository;
  private readonly JoinFamilyHandler _handler;
  private readonly IAppRepository<User> _userAppRepository;

  public JoinFamilyHandlerTests()
  {
    _familyAppRepository = Substitute.For<IAppRepository<Family>>();
    _userAppRepository = Substitute.For<IAppRepository<User>>();
    _handler = new(_familyAppRepository, _userAppRepository);
  }

  [Fact]
  public async Task Handle_ValidCommand_AddsMemberToFamily()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var user = new User(123456789, "John Doe");
    var family = new Family("Smith Family", "UTC");
    var command = new JoinFamilyCommand(userId, familyId, FamilyRole.Child);

    _userAppRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);
    _familyAppRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    family.Members.Count.ShouldBe(1);
    var member = family.Members.First();
    result.Value.ShouldBe(member.Id);
    member.UserId.ShouldBe(user.Id);
    member.Role.ShouldBe(FamilyRole.Child);
    await _familyAppRepository.Received(1).UpdateAsync(family, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_NonExistentUser_ReturnsNotFound()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var command = new JoinFamilyCommand(userId, familyId, FamilyRole.Child);

    _userAppRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns((User?)null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_NonExistentFamily_ReturnsNotFound()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var user = new User(123456789, "John Doe");
    var command = new JoinFamilyCommand(userId, familyId, FamilyRole.Child);

    _userAppRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);
    _familyAppRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns((Family?)null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_UserAlreadyMember_ReturnsError()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var user = new User(123456789, "John Doe");
    var family = new Family("Smith Family", "UTC");
    var existingUser = TestHelpers.CreateUser();
    family.AddMember(existingUser, FamilyRole.Adult);
    userId = existingUser.Id;
    var command = new JoinFamilyCommand(userId, familyId, FamilyRole.Child);

    _userAppRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);
    _familyAppRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.Error);
    family.Members.Count.ShouldBe(1); // Still only one member
  }
}
