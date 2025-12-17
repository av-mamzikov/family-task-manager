using Ardalis.Result;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.Services;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.SpotAggregate.Specifications;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Core.TaskAggregate.Specifications;
using FamilyTaskManager.UseCases.Features.TasksManagement.Commands;
using FamilyTaskManager.UseCases.Features.TasksManagement.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FamilyTaskManager.UnitTests.UseCases.TasksManagement.Commands;

public class ProcessScheduledTasksHandlerUnitTests
{
  [Fact]
  public async Task Handle_UsesAssignedMemberSelector_WhenCreatingScheduledTask()
  {
    // Arrange
    var family = new Family("Test Family", "UTC");
    var spot = new Spot(family.Id, SpotType.Cat, "Spot");
    typeof(Spot).GetProperty("Family")!.SetValue(spot, family);

    var schedule = Schedule.CreateDaily(new(19, 0)).Value;
    var template = new TaskTemplate(
      family.Id,
      spot.Id,
      new("Template"),
      new(1),
      schedule,
      DueDuration.FromHours(1),
      Guid.NewGuid());
    typeof(TaskTemplate).GetProperty("Family")!.SetValue(template, family);

    var templateReadRepo = Substitute.For<IAppReadRepository<TaskTemplate>>();
    var taskRepo = Substitute.For<IAppRepository<TaskInstance>>();
    var spotRepo = Substitute.For<IAppRepository<Spot>>();
    var factory = Substitute.For<ITaskInstanceFactory>();
    var assignedMemberSelector = Substitute.For<IAssignedMemberSelector>();

    templateReadRepo.ListAsync(Arg.Any<TaskTemplatesWithFamilyAndScheduleSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplate> { template });

    spotRepo.FirstOrDefaultAsync(Arg.Any<GetSpotByIdWithFamilySpec>(), Arg.Any<CancellationToken>())
      .Returns(spot);

    taskRepo.ListAsync(Arg.Any<ActiveTaskInstancesByTemplateSpec>(), Arg.Any<CancellationToken>())
      .Returns([]);

    var assigned = family.AddMember(new(1, "name"), FamilyRole.Admin);
    assignedMemberSelector.SelectAssignedMemberAsync(template, spot, Arg.Any<CancellationToken>())
      .Returns(ValueTask.FromResult<FamilyMember?>(assigned));

    // dueAt = trigger (19:00) + dueDuration (1 hour) => 20:00
    var today = DateTime.UtcNow.Date;
    var checkFrom = today.AddHours(18);
    var checkTo = today.AddHours(20);

    var expectedDueAt = today.AddHours(20);

    var createdTask =
      new TaskInstance(spot, template.Title.Value, template.Points, expectedDueAt, template.Id, assigned);

    factory.CreateFromTemplate(template, spot, expectedDueAt, Arg.Any<IEnumerable<TaskInstance>>(), assigned)
      .Returns(Result.Success(createdTask));

    var sut = new ProcessScheduledTasksHandler(
      templateReadRepo,
      taskRepo,
      spotRepo,
      factory,
      NullLogger<ProcessScheduledTasksHandler>.Instance,
      assignedMemberSelector);

    // Act
    var result = await sut.Handle(new(checkFrom, checkTo), CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBe(1);

    await assignedMemberSelector.Received(1)
      .SelectAssignedMemberAsync(template, spot, Arg.Any<CancellationToken>());

    factory.Received(1)
      .CreateFromTemplate(template, spot, expectedDueAt, Arg.Any<IEnumerable<TaskInstance>>(), assigned);
  }
}
