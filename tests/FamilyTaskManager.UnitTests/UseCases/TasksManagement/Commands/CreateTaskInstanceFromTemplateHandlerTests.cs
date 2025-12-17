using Ardalis.Result;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.Services;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.SpotAggregate.Specifications;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Core.TaskAggregate.Specifications;
using FamilyTaskManager.UseCases.Features.TasksManagement.Commands;
using FamilyTaskManager.UseCases.Features.TasksManagement.Services;

namespace FamilyTaskManager.UnitTests.UseCases.TasksManagement.Commands;

public class CreateTaskInstanceFromTemplateHandlerTests
{
  [Fact]
  public async Task Handle_UsesAssignedMemberSelector_AndPassesAssignedMemberIdToFactory()
  {
    // Arrange
    var family = new Family("Test Family", "UTC");
    var spot = new Spot(family.Id, SpotType.Cat, "Spot");
    typeof(Spot).GetProperty("Family")!.SetValue(spot, family);

    var template = new TaskTemplate(
      family.Id,
      spot.Id,
      new("Template"),
      new(1),
      Schedule.CreateManual().Value,
      DueDuration.FromHours(1),
      Guid.NewGuid());

    var templateRepo = Substitute.For<IAppRepository<TaskTemplate>>();
    var taskRepo = Substitute.For<IAppRepository<TaskInstance>>();
    var spotRepo = Substitute.For<IAppRepository<Spot>>();
    var factory = Substitute.For<ITaskInstanceFactory>();
    var moodCalculator = Substitute.For<ISpotMoodCalculator>();
    var assignedMemberSelector = Substitute.For<IAssignedMemberSelector>();

    var dueAt = DateTime.UtcNow.AddHours(2);
    var assignedId = Guid.NewGuid();

    templateRepo.GetByIdAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);
    spotRepo.FirstOrDefaultAsync(Arg.Any<GetSpotByIdWithFamilySpec>(), Arg.Any<CancellationToken>()).Returns(spot);
    taskRepo.ListAsync(Arg.Any<TaskInstancesByTemplateSpec>(), Arg.Any<CancellationToken>()).Returns([]);

    assignedMemberSelector
      .SelectAssignedMemberIdAsync(template, spot, Arg.Any<CancellationToken>())
      .Returns(ValueTask.FromResult<Guid?>(assignedId));

    var createdTask = new TaskInstance(spot, template.Title.Value, template.Points, dueAt, template.Id, assignedId);

    factory
      .CreateFromTemplate(template, spot, dueAt, Arg.Any<IEnumerable<TaskInstance>>(), assignedId)
      .Returns(Result.Success(createdTask));

    moodCalculator.CalculateMoodScoreAsync(spot.Id, Arg.Any<CancellationToken>()).Returns(100);

    var sut = new CreateTaskInstanceFromTemplateHandler(
      templateRepo,
      taskRepo,
      factory,
      spotRepo,
      moodCalculator,
      assignedMemberSelector);

    // Act
    var result = await sut.Handle(new(template.Id, dueAt), CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();

    await assignedMemberSelector.Received(1)
      .SelectAssignedMemberIdAsync(template, spot, Arg.Any<CancellationToken>());

    factory.Received(1).CreateFromTemplate(template, spot, dueAt, Arg.Any<IEnumerable<TaskInstance>>(), assignedId);

    await taskRepo.Received(1)
      .AddAsync(Arg.Is<TaskInstance>(t => t.AssignedToMemberId == assignedId), Arg.Any<CancellationToken>());
  }
}
