using Ardalis.Result;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.UseCases.Families.Specifications;
using FamilyTaskManager.UseCases.Statistics;
using FamilyTaskManager.UseCases.Users.Specifications;

namespace FamilyTaskManager.UnitTests.UseCases.Statistics;

public class GetLeaderboardHandlerTests
{
  private readonly IRepository<Family> _familyRepository;
  private readonly GetLeaderboardHandler _handler;
  private readonly IRepository<User> _userRepository;

  public GetLeaderboardHandlerTests()
  {
    _familyRepository = Substitute.For<IRepository<Family>>();
    _userRepository = Substitute.For<IRepository<User>>();
    _handler = new GetLeaderboardHandler(_familyRepository, _userRepository);
  }

  [Fact]
  public async Task Handle_ValidQuery_ReturnsLeaderboardSortedByPoints()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var user1Id = Guid.NewGuid();
    var user2Id = Guid.NewGuid();
    var user3Id = Guid.NewGuid();

    var family = new Family("Smith Family", "UTC");
    var member1 = family.AddMember(user1Id, FamilyRole.Child);
    var member2 = family.AddMember(user2Id, FamilyRole.Adult);
    var member3 = family.AddMember(user3Id, FamilyRole.Child);

    member1.AddPoints(50);
    member2.AddPoints(100);
    member3.AddPoints(25);

    var users = new List<User> { new(111, "Alice"), new(222, "Bob"), new(333, "Charlie") };

    // Set user IDs via reflection (since they're generated in constructor)
    typeof(User).GetProperty("Id")!.SetValue(users[0], user1Id);
    typeof(User).GetProperty("Id")!.SetValue(users[1], user2Id);
    typeof(User).GetProperty("Id")!.SetValue(users[2], user3Id);

    var query = new GetLeaderboardQuery(familyId);

    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);
    _userRepository.ListAsync(Arg.Any<GetUsersByIdsSpec>(), Arg.Any<CancellationToken>())
      .Returns(users);

    // Act
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.Count.ShouldBe(3);

    // Should be sorted by points descending
    result.Value[0].Points.ShouldBe(100);
    result.Value[0].UserId.ShouldBe(user2Id);
    result.Value[1].Points.ShouldBe(50);
    result.Value[1].UserId.ShouldBe(user1Id);
    result.Value[2].Points.ShouldBe(25);
    result.Value[2].UserId.ShouldBe(user3Id);
  }

  [Fact]
  public async Task Handle_NonExistentFamily_ReturnsNotFound()
  {
    // Arrange
    var query = new GetLeaderboardQuery(Guid.NewGuid());

    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns((Family?)null);

    // Act
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_LeaderboardDisabled_ReturnsError()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC", false); // Leaderboard disabled
    var query = new GetLeaderboardQuery(familyId);

    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.Error);
  }

  [Fact]
  public async Task Handle_OnlyActiveMembers_ExcludesInactiveMembers()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var user1Id = Guid.NewGuid();
    var user2Id = Guid.NewGuid();

    var family = new Family("Smith Family", "UTC");
    var member1 = family.AddMember(user1Id, FamilyRole.Child);
    var member2 = family.AddMember(user2Id, FamilyRole.Child);

    member1.AddPoints(50);
    member2.AddPoints(100);
    member2.Deactivate(); // Deactivate second member

    var users = new List<User> { new(111, "Alice") };
    typeof(User).GetProperty("Id")!.SetValue(users[0], user1Id);

    var query = new GetLeaderboardQuery(familyId);

    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);
    _userRepository.ListAsync(Arg.Any<GetUsersByIdsSpec>(), Arg.Any<CancellationToken>())
      .Returns(users);

    // Act
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.Count.ShouldBe(1); // Only active member
    result.Value[0].UserId.ShouldBe(user1Id);
  }
}
