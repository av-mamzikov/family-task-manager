using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.UseCases.Features.TasksManagement.Services;

namespace FamilyTaskManager.UnitTests.UseCases.TasksManagement.Services;

public class AssignedMemberSelectorTests
{
  private static (Family family, Spot spot, TaskTemplate template, FamilyMember m1, FamilyMember m2) CreateBase()
  {
    var family = new Family("Test Family", "UTC");
    var spot = new Spot(family.Id, SpotType.Cat, "Spot");

    var schedule = Schedule.CreateManual().Value;
    var template = new TaskTemplate(
      family.Id,
      spot.Id,
      new("Template"),
      new(1),
      schedule,
      DueDuration.FromHours(1),
      Guid.NewGuid());

    var m1 = family.AddMember(new(1, "u1"), FamilyRole.Admin);
    var m2 = family.AddMember(new(2, "u2"), FamilyRole.Admin);

    return (family, spot, template, m1, m2);
  }

  [Fact]
  public async Task SelectAssignedMemberIdAsync_WhenTemplateHasResponsibles_UsesTemplateCandidatesAndQuery()
  {
    // Arrange
    var (_, spot, template, m1, m2) = CreateBase();
    template.AssignResponsible(m1);
    template.AssignResponsible(m2);
    spot.AssignResponsible(m1);

    var statsQuery = Substitute.For<ITaskCompletionStatsQuery>();
    statsQuery.GetLastCompletedAtByMemberForTemplateAsync(
        template.FamilyId,
        template.Id,
        Arg.Any<IReadOnlyCollection<Guid>>(),
        Arg.Any<CancellationToken>())
      .Returns(ValueTask.FromResult<IReadOnlyDictionary<Guid, DateTime>>(
        new Dictionary<Guid, DateTime> { { m1.Id, DateTime.UtcNow } }));

    var sut = new AssignedMemberSelector(statsQuery);

    // Act
    var result = await sut.SelectAssignedMemberAsync(template, spot, CancellationToken.None);

    // Assert
    result!.Id.ShouldBe(m2.Id);

    await statsQuery.Received(1).GetLastCompletedAtByMemberForTemplateAsync(
      template.FamilyId,
      template.Id,
      Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2 && ids.Contains(m1.Id) && ids.Contains(m2.Id)),
      Arg.Any<CancellationToken>());

    await statsQuery.DidNotReceive().GetLastCompletedAtByMemberForSpotAsync(
      Arg.Any<Guid>(),
      Arg.Any<Guid>(),
      Arg.Any<IReadOnlyCollection<Guid>>(),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task SelectAssignedMemberIdAsync_WhenNoTemplateResponsibles_FallsBackToSpotResponsibles()
  {
    // Arrange
    var (_, spot, template, m1, m2) = CreateBase();
    spot.AssignResponsible(m1);
    spot.AssignResponsible(m2);

    var older = DateTime.UtcNow.AddDays(-10);
    var newer = DateTime.UtcNow.AddDays(-1);

    var statsQuery = Substitute.For<ITaskCompletionStatsQuery>();
    statsQuery.GetLastCompletedAtByMemberForSpotAsync(
        spot.FamilyId,
        spot.Id,
        Arg.Any<IReadOnlyCollection<Guid>>(),
        Arg.Any<CancellationToken>())
      .Returns(ValueTask.FromResult<IReadOnlyDictionary<Guid, DateTime>>(
        new Dictionary<Guid, DateTime> { { m1.Id, newer }, { m2.Id, older } }));

    var sut = new AssignedMemberSelector(statsQuery);

    // Act
    var result = await sut.SelectAssignedMemberAsync(template, spot, CancellationToken.None);

    // Assert
    result!.Id.ShouldBe(m2.Id);
  }

  [Fact]
  public async Task SelectAssignedMemberIdAsync_WhenNoResponsibles_ReturnsNull()
  {
    // Arrange
    var (_, spot, template, _, _) = CreateBase();
    var statsQuery = Substitute.For<ITaskCompletionStatsQuery>();
    var sut = new AssignedMemberSelector(statsQuery);

    // Act
    var result = await sut.SelectAssignedMemberAsync(template, spot, CancellationToken.None);

    // Assert
    result.ShouldBeNull();
  }
}
