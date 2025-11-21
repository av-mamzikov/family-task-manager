using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.UseCases.Families;
using FamilyTaskManager.UseCases.Families.Specifications;

namespace FamilyTaskManager.UnitTests.UseCases.Families;

public class JoinByInviteCodeHandlerTests
{
  private readonly IRepository<Family> _familyRepository;
  private readonly IRepository<User> _userRepository;
  private readonly IRepository<Invitation> _invitationRepository;
  private readonly JoinByInviteCodeHandler _handler;

  public JoinByInviteCodeHandlerTests()
  {
    _familyRepository = Substitute.For<IRepository<Family>>();
    _userRepository = Substitute.For<IRepository<User>>();
    _invitationRepository = Substitute.For<IRepository<Invitation>>();
    _handler = new JoinByInviteCodeHandler(_familyRepository, _userRepository, _invitationRepository);
  }

  [Fact]
  public async Task Handle_ValidInvitation_AddsUserToFamily()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var user = new User(123456789, "John Doe");
    var family = new Family("Test Family", "UTC", true);
    var invitation = new Invitation(familyId, FamilyRole.Adult, Guid.NewGuid(), 7);
    
    var command = new JoinByInviteCodeCommand(userId, invitation.Code);
    
    _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);
    
    _invitationRepository.FirstOrDefaultAsync(Arg.Any<GetInvitationByCodeSpec>(), Arg.Any<CancellationToken>())
      .Returns(invitation);
    
    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    family.Members.Count.ShouldBe(1);
    family.Members.First().UserId.ShouldBe(userId);
    family.Members.First().Role.ShouldBe(FamilyRole.Adult);
    invitation.IsActive.ShouldBeFalse();
    await _familyRepository.Received(1).UpdateAsync(family, Arg.Any<CancellationToken>());
    await _invitationRepository.Received(1).UpdateAsync(invitation, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_NonExistentUser_ReturnsNotFound()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var command = new JoinByInviteCodeCommand(userId, "TESTCODE");
    
    _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns((User?)null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(Ardalis.Result.ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_NonExistentInvitation_ReturnsNotFound()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var user = new User(123456789, "John Doe");
    var command = new JoinByInviteCodeCommand(userId, "INVALID");
    
    _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);
    
    _invitationRepository.FirstOrDefaultAsync(Arg.Any<GetInvitationByCodeSpec>(), Arg.Any<CancellationToken>())
      .Returns((Invitation?)null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(Ardalis.Result.ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_ExpiredInvitation_ReturnsError()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var user = new User(123456789, "John Doe");
    var invitation = new Invitation(familyId, FamilyRole.Adult, Guid.NewGuid(), -1); // Expired
    
    var command = new JoinByInviteCodeCommand(userId, invitation.Code);
    
    _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);
    
    _invitationRepository.FirstOrDefaultAsync(Arg.Any<GetInvitationByCodeSpec>(), Arg.Any<CancellationToken>())
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
    var invitation = new Invitation(familyId, FamilyRole.Adult, Guid.NewGuid(), 7);
    invitation.Deactivate();
    
    var command = new JoinByInviteCodeCommand(userId, invitation.Code);
    
    _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);
    
    _invitationRepository.FirstOrDefaultAsync(Arg.Any<GetInvitationByCodeSpec>(), Arg.Any<CancellationToken>())
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
    var family = new Family("Test Family", "UTC", true);
    family.AddMember(userId, FamilyRole.Child); // User already in family
    var invitation = new Invitation(familyId, FamilyRole.Adult, Guid.NewGuid(), 7);
    
    var command = new JoinByInviteCodeCommand(userId, invitation.Code);
    
    _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);
    
    _invitationRepository.FirstOrDefaultAsync(Arg.Any<GetInvitationByCodeSpec>(), Arg.Any<CancellationToken>())
      .Returns(invitation);
    
    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
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
    var invitation = new Invitation(familyId, FamilyRole.Adult, Guid.NewGuid(), 7);
    
    var command = new JoinByInviteCodeCommand(userId, invitation.Code);
    
    _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);
    
    _invitationRepository.FirstOrDefaultAsync(Arg.Any<GetInvitationByCodeSpec>(), Arg.Any<CancellationToken>())
      .Returns(invitation);
    
    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns((Family?)null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(Ardalis.Result.ResultStatus.NotFound);
  }
}
