using Ardalis.Result;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.UnitTests.Helpers;
using FamilyTaskManager.UseCases.Families;
using FamilyTaskManager.UseCases.Families.Specifications;

namespace FamilyTaskManager.UnitTests.UseCases.Families;

public class JoinByInviteCodeHandlerTests
{
  private readonly IAppRepository<Family> _familyAppRepository;
  private readonly JoinByInviteCodeHandler _handler;
  private readonly IAppRepository<Invitation> _invitationAppRepository;
  private readonly IAppRepository<User> _userAppRepository;

  public JoinByInviteCodeHandlerTests()
  {
    _familyAppRepository = Substitute.For<IAppRepository<Family>>();
    _userAppRepository = Substitute.For<IAppRepository<User>>();
    _invitationAppRepository = Substitute.For<IAppRepository<Invitation>>();
    _handler = new(_familyAppRepository, _userAppRepository, _invitationAppRepository);
  }

  [Fact]
  public async Task Handle_ValidInvitation_AddsUserToFamily()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var user = new User(123456789, "John Doe");
    var family = new Family("Test Family", "UTC");
    var invitation = new Invitation(familyId, FamilyRole.Adult, Guid.NewGuid());

    var command = new JoinByInviteCodeCommand(userId, invitation.Code);

    _userAppRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);

    _invitationAppRepository.FirstOrDefaultAsync(Arg.Any<GetInvitationByCodeSpec>(), Arg.Any<CancellationToken>())
      .Returns(invitation);

    _familyAppRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    family.Members.Count.ShouldBe(1);
    family.Members.First().UserId.ShouldBe(user.Id);
    family.Members.First().Role.ShouldBe(FamilyRole.Adult);
    invitation.IsActive.ShouldBeFalse();
    await _familyAppRepository.Received(1).UpdateAsync(family, Arg.Any<CancellationToken>());
    await _invitationAppRepository.Received(1).UpdateAsync(invitation, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_NonExistentUser_ReturnsNotFound()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var command = new JoinByInviteCodeCommand(userId, "TESTCODE");

    _userAppRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns((User?)null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_NonExistentInvitation_ReturnsNotFound()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var user = new User(123456789, "John Doe");
    var command = new JoinByInviteCodeCommand(userId, "INVALID");

    _userAppRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);

    _invitationAppRepository.FirstOrDefaultAsync(Arg.Any<GetInvitationByCodeSpec>(), Arg.Any<CancellationToken>())
      .Returns((Invitation?)null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_ExpiredInvitation_ReturnsError()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var user = new User(123456789, "John Doe");

    // Create an invitation that's already expired by using negative expiration days
    var invitation = new Invitation(familyId, FamilyRole.Adult, Guid.NewGuid(), -1);

    var command = new JoinByInviteCodeCommand(userId, invitation.Code);

    _userAppRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);

    _invitationAppRepository.FirstOrDefaultAsync(Arg.Any<GetInvitationByCodeSpec>(), Arg.Any<CancellationToken>())
      .Returns(invitation);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Errors.ShouldContain("Invitation has expired");
  }

  [Fact]
  public async Task Handle_InactiveInvitation_ReturnsError()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var user = new User(123456789, "John Doe");
    var invitation = new Invitation(familyId, FamilyRole.Adult, Guid.NewGuid());
    invitation.Deactivate();

    var command = new JoinByInviteCodeCommand(userId, invitation.Code);

    _userAppRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);

    _invitationAppRepository.FirstOrDefaultAsync(Arg.Any<GetInvitationByCodeSpec>(), Arg.Any<CancellationToken>())
      .Returns(invitation);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Errors.ShouldContain("Invitation is not active");
  }

  [Fact]
  public async Task Handle_UserAlreadyMember_ReturnsError()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var user = new User(123456789, "John Doe");
    var family = new Family("Test Family", "UTC");
    var existingUser = TestHelpers.CreateUser();
    family.AddMember(existingUser, FamilyRole.Child); // User already in family
    userId = existingUser.Id;
    var invitation = new Invitation(familyId, FamilyRole.Adult, Guid.NewGuid());

    var command = new JoinByInviteCodeCommand(userId, invitation.Code);

    _userAppRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);

    _invitationAppRepository.FirstOrDefaultAsync(Arg.Any<GetInvitationByCodeSpec>(), Arg.Any<CancellationToken>())
      .Returns(invitation);

    _familyAppRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Errors.ShouldContain("User is already a member of this family");
  }

  [Fact]
  public async Task Handle_NonExistentFamily_ReturnsNotFound()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var user = new User(123456789, "John Doe");
    var invitation = new Invitation(familyId, FamilyRole.Adult, Guid.NewGuid());

    var command = new JoinByInviteCodeCommand(userId, invitation.Code);

    _userAppRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);

    _invitationAppRepository.FirstOrDefaultAsync(Arg.Any<GetInvitationByCodeSpec>(), Arg.Any<CancellationToken>())
      .Returns(invitation);

    _familyAppRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns((Family?)null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.NotFound);
  }
}
