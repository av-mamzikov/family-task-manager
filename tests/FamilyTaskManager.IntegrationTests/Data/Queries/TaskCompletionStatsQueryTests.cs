using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.Infrastructure.Data.Queries;

namespace FamilyTaskManager.IntegrationTests.Data.Queries;

public class TaskCompletionStatsQueryTests : BaseRepositoryTestFixture
{
  [Fact]
  public async Task GetLastCompletedAtByMemberForTemplateAsync_ReturnsMaxCreatedAtPerAssignee()
  {
    // Arrange
    var family = new Family("Test Family", "UTC");
    var spot = new Spot(family.Id, SpotType.Cat, "Test Spot");
    typeof(Spot).GetProperty("Family")!.SetValue(spot, family);

    var schedule = Schedule.CreateManual().Value;
    var template = new TaskTemplate(
      family.Id,
      spot.Id,
      new("Template 1"),
      new(1),
      schedule,
      DueDuration.FromHours(1),
      Guid.NewGuid());

    var user1 = new User(1001, "User 1");
    var user2 = new User(1002, "User 2");
    var user3 = new User(1003, "User 3");

    var member1 = family.AddMember(user1, FamilyRole.Admin);
    var member2 = family.AddMember(user2, FamilyRole.Admin);
    var member3 = family.AddMember(user3, FamilyRole.Admin);

    await DbContext.Families.AddAsync(family);
    await DbContext.Spots.AddAsync(spot);
    await DbContext.TaskTemplates.AddAsync(template);
    await DbContext.SaveChangesAsync();

    var dueAt = DateTime.UtcNow.AddDays(1);

    var m1Old = DateTime.UtcNow.AddDays(-10);
    var m1New = DateTime.UtcNow.AddDays(-2);
    var m2 = DateTime.UtcNow.AddDays(-5);
    var m3 = DateTime.UtcNow.AddDays(-1);

    var task1 = new TaskInstance(spot, "t1", new(1), dueAt, template.Id, member1);
    typeof(TaskInstance).GetProperty(nameof(TaskInstance.CreatedAt))!.SetValue(task1, m1Old);

    var task2 = new TaskInstance(spot, "t2", new(1), dueAt, template.Id, member1);
    typeof(TaskInstance).GetProperty(nameof(TaskInstance.CreatedAt))!.SetValue(task2, m1New);

    var task3 = new TaskInstance(spot, "t3", new(1), dueAt, template.Id, member2);
    typeof(TaskInstance).GetProperty(nameof(TaskInstance.CreatedAt))!.SetValue(task3, m2);

    var task4 = new TaskInstance(spot, "t4", new(1), dueAt, template.Id, member3);
    typeof(TaskInstance).GetProperty(nameof(TaskInstance.CreatedAt))!.SetValue(task4, m3);

    // Not assigned -> must be ignored
    var taskNotCompleted = new TaskInstance(spot, "t5", new(1), dueAt, template.Id);

    // Different template -> must be ignored
    var otherSchedule = Schedule.CreateManual().Value;
    var otherTemplate = new TaskTemplate(
      family.Id,
      spot.Id,
      new("Template 2"),
      new(1),
      otherSchedule,
      DueDuration.FromHours(1),
      Guid.NewGuid());

    await DbContext.TaskTemplates.AddAsync(otherTemplate);

    var taskOtherTemplate = new TaskInstance(spot, "t6", new(1), dueAt, otherTemplate.Id, member1);
    typeof(TaskInstance).GetProperty(nameof(TaskInstance.CreatedAt))!.SetValue(taskOtherTemplate,
      DateTime.UtcNow.AddDays(-3));

    await DbContext.TaskInstances.AddRangeAsync(task1, task2, task3, task4, taskNotCompleted, taskOtherTemplate);
    await DbContext.SaveChangesAsync();

    var sut = new TaskCompletionStatsQuery(DbContext);

    // Act
    var result = await sut.GetLastCreatedAtByAssignedForTemplateAsync(
      family.Id,
      template.Id,
      new[] { member1.Id, member2.Id },
      CancellationToken.None);

    // Assert
    result.Count.ShouldBe(2);
    result[member1.Id].ShouldBe(m1New, TimeSpan.FromMilliseconds(1));
    result[member2.Id].ShouldBe(m2, TimeSpan.FromMilliseconds(1));
    result.ContainsKey(member3.Id).ShouldBeFalse();
  }
}
