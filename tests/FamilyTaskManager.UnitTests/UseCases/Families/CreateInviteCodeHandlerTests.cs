using Ardalis.Result;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.UnitTests.Helpers;
using FamilyTaskManager.UseCases.Families;
using FamilyTaskManager.UseCases.Families.Specifications;

namespace FamilyTaskManager.UnitTests.UseCases.Families;

public class CreateInviteCodeHandlerTests
{
  private readonly IAppRepository<Family> _familyAppRepository;
  private readonly CreateInviteCodeHandler _handler;
  private readonly IAppRepository<Invitation> _invitationAppRepository;

  public CreateInviteCodeHandlerTests()
  {
    _familyAppRepository = Substitute.For<IAppRepository<Family>>();
    _invitationAppRepository = Substitute.For<IAppRepository<Invitation>>();
    _handler = new(_familyAppRepository, _invitationAppRepository);
  }

  [Fact]
  public async Task Handle_AdminUser_CreatesInvitationSuccessfully()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var family = new Family("Test Family", "UTC");
    var user = TestHelpers.CreateUser();
    family.AddMember(user, FamilyRole.Admin);
    userId = user.Id;

    var command = new CreateInviteCodeCommand(familyId, FamilyRole.Adult, userId);

    _familyAppRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);

    Invitation? capturedInvitation = null;
    await _invitationAppRepository.AddAsync(Arg.Do<Invitation>(i => capturedInvitation = i),
      Arg.Any<CancellationToken>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldNotBeNullOrEmpty();
    capturedInvitation.ShouldNotBeNull();
    capturedInvitation.FamilyId.ShouldBe(familyId);
    capturedInvitation.Role.ShouldBe(FamilyRole.Adult);
    capturedInvitation.CreatedBy.ShouldBe(userId);
    capturedInvitation.IsActive.ShouldBeTrue();
    capturedInvitation.Code.Length.ShouldBe(8);
  }

  [Fact]
  public async Task Handle_NonExistentFamily_ReturnsNotFound()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var command = new CreateInviteCodeCommand(familyId, FamilyRole.Adult, userId);

    _familyAppRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns((Family?)null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.NotFound);
    await _invitationAppRepository.DidNotReceive().AddAsync(Arg.Any<Invitation>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_NonMemberUser_ReturnsForbidden()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();
    var family = new Family("Test Family", "UTC");
    var otherUser = TestHelpers.CreateUser();
    family.AddMember(otherUser, FamilyRole.Admin);

    var command = new CreateInviteCodeCommand(familyId, FamilyRole.Adult, userId);

    _familyAppRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.Forbidden);
    await _invitationAppRepository.DidNotReceive().AddAsync(Arg.Any<Invitation>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_NonAdminUser_ReturnsForbidden()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var family = new Family("Test Family", "UTC");
    var user = TestHelpers.CreateUser();
    family.AddMember(user, FamilyRole.Adult); // Not Admin
    userId = user.Id;

    var command = new CreateInviteCodeCommand(familyId, FamilyRole.Child, userId);

    _familyAppRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.Forbidden);
    await _invitationAppRepository.DidNotReceive().AddAsync(Arg.Any<Invitation>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_CustomExpirationDays_SetsCorrectExpiration()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var family = new Family("Test Family", "UTC");
    var user = TestHelpers.CreateUser();
    family.AddMember(user, FamilyRole.Admin);
    userId = user.Id;

    var command = new CreateInviteCodeCommand(familyId, FamilyRole.Adult, userId, 14);

    _familyAppRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);

    Invitation? capturedInvitation = null;
    await _invitationAppRepository.AddAsync(Arg.Do<Invitation>(i => capturedInvitation = i),
      Arg.Any<CancellationToken>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedInvitation.ShouldNotBeNull();
    capturedInvitation.ExpiresAt.ShouldNotBeNull();
    capturedInvitation.ExpiresAt.Value.ShouldBeGreaterThan(DateTime.UtcNow.AddDays(13));
    capturedInvitation.ExpiresAt.Value.ShouldBeLessThan(DateTime.UtcNow.AddDays(15));
  }
}
