using Ardalis.Result;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.UseCases.Families;
using FamilyTaskManager.UseCases.Families.Specifications;

namespace FamilyTaskManager.UnitTests.UseCases.Families;

public class CreateInviteCodeHandlerTests
{
  private readonly IRepository<Family> _familyRepository;
  private readonly CreateInviteCodeHandler _handler;
  private readonly IRepository<Invitation> _invitationRepository;

  public CreateInviteCodeHandlerTests()
  {
    _familyRepository = Substitute.For<IRepository<Family>>();
    _invitationRepository = Substitute.For<IRepository<Invitation>>();
    _handler = new CreateInviteCodeHandler(_familyRepository, _invitationRepository);
  }

  [Fact]
  public async Task Handle_AdminUser_CreatesInvitationSuccessfully()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var family = new Family("Test Family", "UTC");
    family.AddMember(userId, FamilyRole.Admin);

    var command = new CreateInviteCodeCommand(familyId, FamilyRole.Adult, userId);

    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);

    Invitation? capturedInvitation = null;
    await _invitationRepository.AddAsync(Arg.Do<Invitation>(i => capturedInvitation = i), Arg.Any<CancellationToken>());

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

    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns((Family?)null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.NotFound);
    await _invitationRepository.DidNotReceive().AddAsync(Arg.Any<Invitation>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_NonMemberUser_ReturnsForbidden()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();
    var family = new Family("Test Family", "UTC");
    family.AddMember(otherUserId, FamilyRole.Admin);

    var command = new CreateInviteCodeCommand(familyId, FamilyRole.Adult, userId);

    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.Forbidden);
    await _invitationRepository.DidNotReceive().AddAsync(Arg.Any<Invitation>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_NonAdminUser_ReturnsForbidden()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var family = new Family("Test Family", "UTC");
    family.AddMember(userId, FamilyRole.Adult); // Not Admin

    var command = new CreateInviteCodeCommand(familyId, FamilyRole.Child, userId);

    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.Forbidden);
    await _invitationRepository.DidNotReceive().AddAsync(Arg.Any<Invitation>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_CustomExpirationDays_SetsCorrectExpiration()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var family = new Family("Test Family", "UTC");
    family.AddMember(userId, FamilyRole.Admin);

    var command = new CreateInviteCodeCommand(familyId, FamilyRole.Adult, userId, 14);

    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);

    Invitation? capturedInvitation = null;
    await _invitationRepository.AddAsync(Arg.Do<Invitation>(i => capturedInvitation = i), Arg.Any<CancellationToken>());

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
