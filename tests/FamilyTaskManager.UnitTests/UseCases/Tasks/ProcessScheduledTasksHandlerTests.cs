using Ardalis.Result;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Core.Services;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.UseCases.Tasks;
using FamilyTaskManager.UseCases.Tasks.Specifications;
using FamilyTaskManager.UseCases.TaskTemplates;
using FamilyTaskManager.UseCases.TaskTemplates.Specifications;

namespace FamilyTaskManager.UnitTests.UseCases.Tasks;

public class ProcessScheduledTasksHandlerTests
{
  private readonly ProcessScheduledTasksHandler _handler;
  private readonly ILogger<ProcessScheduledTasksHandler> _logger;
  private readonly IScheduleEvaluator _scheduleEvaluator;
  private readonly ITaskInstanceFactory _taskInstanceFactory;
  private readonly IRepository<TaskInstance> _taskRepository;
  private readonly IReadRepository<TaskTemplate> _templateRepository;

  public ProcessScheduledTasksHandlerTests()
  {
    _templateRepository = Substitute.For<IReadRepository<TaskTemplate>>();
    _taskRepository = Substitute.For<IRepository<TaskInstance>>();
    _taskInstanceFactory = Substitute.For<ITaskInstanceFactory>();
    _scheduleEvaluator = Substitute.For<IScheduleEvaluator>();
    _logger = Substitute.For<ILogger<ProcessScheduledTasksHandler>>();

    _handler = new ProcessScheduledTasksHandler(
      _templateRepository,
      _taskRepository,
      _taskInstanceFactory,
      _scheduleEvaluator,
      _logger);
  }

  [Fact]
  public async Task Handle_NoActiveTemplates_ReturnsZeroCreated()
  {
    // Arrange
    var command = new ProcessScheduledTaskCommand(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));

    _templateRepository.ListAsync(Arg.Any<ActiveTaskTemplatesWithTimeZoneSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplateDto>());

    _templateRepository.ListAsync(Arg.Any<TaskTemplatesWithFamilyByIdsSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplate>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBe(0);
  }

  [Fact]
  public async Task Handle_TemplateNotTriggeredInWindow_DoesNotCreateInstance()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var templateId = Guid.NewGuid();
    var checkFrom = DateTime.UtcNow;
    var checkTo = checkFrom.AddHours(1);

    var command = new ProcessScheduledTaskCommand(checkFrom, checkTo);

    var templateDto = new TaskTemplateDto(
      templateId,
      familyId,
      "Daily Task",
      10,
      "0 9 * * *",
      petId,
      "Test Pet",
      true,
      DateTime.UtcNow,
      TimeSpan.FromHours(24));
    var family = new Family("Test Family", "UTC", false);
    var template = new TaskTemplate(familyId, petId, "Daily Task", 10, "0 9 * * *", TimeSpan.FromHours(24),
      Guid.NewGuid());
    typeof(TaskTemplate).GetProperty(nameof(TaskTemplate.Family))!.SetValue(template, family);

    _templateRepository.ListAsync(Arg.Any<ActiveTaskTemplatesWithTimeZoneSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplateDto> { templateDto });

    _templateRepository.ListAsync(Arg.Any<TaskTemplatesWithFamilyByIdsSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplate> { template });

    _scheduleEvaluator.ShouldTriggerInWindow(template.Schedule, checkFrom, checkTo, family.Timezone)
      .Returns((DateTime?)null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBe(0);
    await _taskRepository.DidNotReceive().AddAsync(Arg.Any<TaskInstance>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_TemplateTriggeredInWindow_CreatesTaskInstance()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var templateId = Guid.NewGuid();
    var checkFrom = DateTime.UtcNow;
    var checkTo = checkFrom.AddHours(1);
    var triggerTime = checkFrom.AddMinutes(30);

    var command = new ProcessScheduledTaskCommand(checkFrom, checkTo);

    var templateDto = new TaskTemplateDto(
      templateId,
      familyId,
      "Daily Task",
      10,
      "0 9 * * *",
      petId,
      "Test Pet",
      true,
      DateTime.UtcNow,
      TimeSpan.FromHours(24));
    var family = new Family("Test Family", "UTC", false);
    var template = new TaskTemplate(familyId, petId, "Daily Task", 10, "0 9 * * *", TimeSpan.FromHours(24),
      Guid.NewGuid());
    typeof(TaskTemplate).GetProperty(nameof(TaskTemplate.Family))!.SetValue(template, family);

    var taskInstance =
      new TaskInstance(familyId, petId, "Daily Task", 10, TaskType.Recurring, triggerTime.AddHours(24));

    _templateRepository.ListAsync(Arg.Any<ActiveTaskTemplatesWithTimeZoneSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplateDto> { templateDto });

    _templateRepository.ListAsync(Arg.Any<TaskTemplatesWithFamilyByIdsSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplate> { template });

    _scheduleEvaluator.ShouldTriggerInWindow(template.Schedule, checkFrom, checkTo, family.Timezone)
      .Returns(triggerTime);

    _taskRepository.ListAsync(Arg.Any<TaskInstancesByTemplateSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskInstance>());

    _taskInstanceFactory
      .CreateFromTemplate(template, triggerTime + template.DueDuration, Arg.Any<IEnumerable<TaskInstance>>())
      .Returns(Result<TaskInstance>.Success(taskInstance));

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBe(1);
    await _taskRepository.Received(1).AddAsync(taskInstance, Arg.Any<CancellationToken>());
    await _taskRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_MultipleTemplatesTriggered_CreatesMultipleInstances()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId1 = Guid.NewGuid();
    var petId2 = Guid.NewGuid();
    var templateId1 = Guid.NewGuid();
    var templateId2 = Guid.NewGuid();
    var checkFrom = DateTime.UtcNow;
    var checkTo = checkFrom.AddHours(1);
    var triggerTime = checkFrom.AddMinutes(30);

    var command = new ProcessScheduledTaskCommand(checkFrom, checkTo);

    var templateDto1 = new TaskTemplateDto(
      templateId1,
      familyId,
      "Task 1",
      10,
      "0 9 * * *",
      petId1,
      "Pet 1",
      true,
      DateTime.UtcNow,
      TimeSpan.FromHours(24));
    var templateDto2 = new TaskTemplateDto(
      templateId2,
      familyId,
      "Task 2",
      15,
      "0 10 * * *",
      petId2,
      "Pet 2",
      true,
      DateTime.UtcNow,
      TimeSpan.FromHours(12));
    var family = new Family("Test Family", "UTC", false);

    var template1 =
      new TaskTemplate(familyId, petId1, "Task 1", 10, "0 9 * * *", TimeSpan.FromHours(24), Guid.NewGuid());
    var template2 = new TaskTemplate(familyId, petId2, "Task 2", 15, "0 10 * * *", TimeSpan.FromHours(12),
      Guid.NewGuid());
    typeof(TaskTemplate).GetProperty(nameof(TaskTemplate.Family))!.SetValue(template1, family);
    typeof(TaskTemplate).GetProperty(nameof(TaskTemplate.Family))!.SetValue(template2, family);

    var taskInstance1 = new TaskInstance(familyId, petId1, "Task 1", 10, TaskType.Recurring, triggerTime.AddHours(24));
    var taskInstance2 = new TaskInstance(familyId, petId2, "Task 2", 15, TaskType.Recurring, triggerTime.AddHours(12));

    _templateRepository.ListAsync(Arg.Any<ActiveTaskTemplatesWithTimeZoneSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplateDto> { templateDto1, templateDto2 });

    _templateRepository.ListAsync(Arg.Any<TaskTemplatesWithFamilyByIdsSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplate> { template1, template2 });

    _scheduleEvaluator.ShouldTriggerInWindow(template1.Schedule, checkFrom, checkTo, family.Timezone)
      .Returns(triggerTime);
    _scheduleEvaluator.ShouldTriggerInWindow(template2.Schedule, checkFrom, checkTo, family.Timezone)
      .Returns(triggerTime);

    _taskRepository.ListAsync(Arg.Any<TaskInstancesByTemplateSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskInstance>());

    _taskInstanceFactory.CreateFromTemplate(template1, Arg.Any<DateTime>(), Arg.Any<IEnumerable<TaskInstance>>())
      .Returns(Result<TaskInstance>.Success(taskInstance1));
    _taskInstanceFactory.CreateFromTemplate(template2, Arg.Any<DateTime>(), Arg.Any<IEnumerable<TaskInstance>>())
      .Returns(Result<TaskInstance>.Success(taskInstance2));

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBe(2);
    await _taskRepository.Received(2).AddAsync(Arg.Any<TaskInstance>(), Arg.Any<CancellationToken>());
    await _taskRepository.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_FactoryReturnsError_DoesNotCreateInstance()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var templateId = Guid.NewGuid();
    var checkFrom = DateTime.UtcNow;
    var checkTo = checkFrom.AddHours(1);
    var triggerTime = checkFrom.AddMinutes(30);

    var command = new ProcessScheduledTaskCommand(checkFrom, checkTo);

    var templateDto = new TaskTemplateDto(
      templateId,
      familyId,
      "Daily Task",
      10,
      "0 9 * * *",
      petId,
      "Test Pet",
      true,
      DateTime.UtcNow,
      TimeSpan.FromHours(24));
    var family = new Family("Test Family", "UTC", false);
    var template = new TaskTemplate(familyId, petId, "Daily Task", 10, "0 9 * * *", TimeSpan.FromHours(24),
      Guid.NewGuid());
    typeof(TaskTemplate).GetProperty(nameof(TaskTemplate.Family))!.SetValue(template, family);

    _templateRepository.ListAsync(Arg.Any<ActiveTaskTemplatesWithTimeZoneSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplateDto> { templateDto });

    _templateRepository.ListAsync(Arg.Any<TaskTemplatesWithFamilyByIdsSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplate> { template });

    _scheduleEvaluator.ShouldTriggerInWindow(template.Schedule, checkFrom, checkTo, family.Timezone)
      .Returns(triggerTime);

    _taskRepository.ListAsync(Arg.Any<TaskInstancesByTemplateSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskInstance>());

    _taskInstanceFactory.CreateFromTemplate(template, Arg.Any<DateTime>(), Arg.Any<IEnumerable<TaskInstance>>())
      .Returns(Result<TaskInstance>.Error("Active instance already exists"));

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBe(0);
    await _taskRepository.DidNotReceive().AddAsync(Arg.Any<TaskInstance>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_TemplateProcessingThrowsException_ContinuesWithOtherTemplates()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId1 = Guid.NewGuid();
    var petId2 = Guid.NewGuid();
    var templateId1 = Guid.NewGuid();
    var templateId2 = Guid.NewGuid();
    var checkFrom = DateTime.UtcNow;
    var checkTo = checkFrom.AddHours(1);
    var triggerTime = checkFrom.AddMinutes(30);

    var command = new ProcessScheduledTaskCommand(checkFrom, checkTo);

    var templateDto1 = new TaskTemplateDto(
      templateId1,
      familyId,
      "Task 1",
      10,
      "0 9 * * *",
      petId1,
      "Pet 1",
      true,
      DateTime.UtcNow,
      TimeSpan.FromHours(24));
    var templateDto2 = new TaskTemplateDto(
      templateId2,
      familyId,
      "Task 2",
      15,
      "0 10 * * *",
      petId2,
      "Pet 2",
      true,
      DateTime.UtcNow,
      TimeSpan.FromHours(12));
    var family = new Family("Test Family", "UTC", false);

    var template1 =
      new TaskTemplate(familyId, petId1, "Task 1", 10, "0 9 * * *", TimeSpan.FromHours(24), Guid.NewGuid());
    var template2 = new TaskTemplate(familyId, petId2, "Task 2", 15, "0 10 * * *", TimeSpan.FromHours(12),
      Guid.NewGuid());
    typeof(TaskTemplate).GetProperty(nameof(TaskTemplate.Family))!.SetValue(template1, family);
    typeof(TaskTemplate).GetProperty(nameof(TaskTemplate.Family))!.SetValue(template2, family);

    var taskInstance2 = new TaskInstance(familyId, petId2, "Task 2", 15, TaskType.Recurring, triggerTime.AddHours(12));

    _templateRepository.ListAsync(Arg.Any<ActiveTaskTemplatesWithTimeZoneSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplateDto> { templateDto1, templateDto2 });

    _templateRepository.ListAsync(Arg.Any<TaskTemplatesWithFamilyByIdsSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplate> { template1, template2 });

    _scheduleEvaluator.ShouldTriggerInWindow(template1.Schedule, checkFrom, checkTo, family.Timezone)
      .Returns(_ => throw new InvalidOperationException("Schedule evaluation failed"));
    _scheduleEvaluator.ShouldTriggerInWindow(template2.Schedule, checkFrom, checkTo, family.Timezone)
      .Returns(triggerTime);

    _taskRepository.ListAsync(Arg.Any<TaskInstancesByTemplateSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskInstance>());

    _taskInstanceFactory.CreateFromTemplate(template2, Arg.Any<DateTime>(), Arg.Any<IEnumerable<TaskInstance>>())
      .Returns(Result<TaskInstance>.Success(taskInstance2));

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBe(1);
    await _taskRepository.Received(1).AddAsync(taskInstance2, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_CalculatesDueAtCorrectly()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var templateId = Guid.NewGuid();
    var checkFrom = DateTime.UtcNow;
    var checkTo = checkFrom.AddHours(1);
    var triggerTime = checkFrom.AddMinutes(30);
    var dueDuration = TimeSpan.FromHours(24);
    var expectedDueAt = triggerTime + dueDuration;

    var command = new ProcessScheduledTaskCommand(checkFrom, checkTo);

    var templateDto = new TaskTemplateDto(
      templateId,
      familyId,
      "Daily Task",
      10,
      "0 9 * * *",
      petId,
      "Test Pet",
      true,
      DateTime.UtcNow,
      TimeSpan.FromHours(24));
    var family = new Family("Test Family", "UTC", false);
    var template = new TaskTemplate(familyId, petId, "Daily Task", 10, "0 9 * * *", dueDuration, Guid.NewGuid());
    typeof(TaskTemplate).GetProperty(nameof(TaskTemplate.Family))!.SetValue(template, family);

    var taskInstance = new TaskInstance(familyId, petId, "Daily Task", 10, TaskType.Recurring, expectedDueAt);

    _templateRepository.ListAsync(Arg.Any<ActiveTaskTemplatesWithTimeZoneSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplateDto> { templateDto });

    _templateRepository.ListAsync(Arg.Any<TaskTemplatesWithFamilyByIdsSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplate> { template });

    _scheduleEvaluator.ShouldTriggerInWindow(template.Schedule, checkFrom, checkTo, family.Timezone)
      .Returns(triggerTime);

    _taskRepository.ListAsync(Arg.Any<TaskInstancesByTemplateSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskInstance>());

    DateTime? capturedDueAt = null;
    _taskInstanceFactory.CreateFromTemplate(
        Arg.Is(template),
        Arg.Do<DateTime>(d => capturedDueAt = d),
        Arg.Any<IEnumerable<TaskInstance>>())
      .Returns(Result<TaskInstance>.Success(taskInstance));

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedDueAt.ShouldNotBeNull();
    capturedDueAt.Value.ShouldBe(expectedDueAt);
  }

  [Fact]
  public async Task Handle_PassesExistingInstancesToFactory()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var templateId = Guid.NewGuid();
    var checkFrom = DateTime.UtcNow;
    var checkTo = checkFrom.AddHours(1);
    var triggerTime = checkFrom.AddMinutes(30);

    var command = new ProcessScheduledTaskCommand(checkFrom, checkTo);

    var templateDto = new TaskTemplateDto(
      templateId,
      familyId,
      "Daily Task",
      10,
      "0 9 * * *",
      petId,
      "Test Pet",
      true,
      DateTime.UtcNow,
      TimeSpan.FromHours(24));
    var family = new Family("Test Family", "UTC", false);
    var template = new TaskTemplate(familyId, petId, "Daily Task", 10, "0 9 * * *", TimeSpan.FromHours(24),
      Guid.NewGuid());
    typeof(TaskTemplate).GetProperty(nameof(TaskTemplate.Family))!.SetValue(template, family);

    var existingInstance =
      new TaskInstance(familyId, petId, "Daily Task", 10, TaskType.Recurring, DateTime.UtcNow.AddDays(-1));
    var existingInstances = new List<TaskInstance> { existingInstance };

    var taskInstance =
      new TaskInstance(familyId, petId, "Daily Task", 10, TaskType.Recurring, triggerTime.AddHours(24));

    _templateRepository.ListAsync(Arg.Any<ActiveTaskTemplatesWithTimeZoneSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplateDto> { templateDto });

    _templateRepository.ListAsync(Arg.Any<TaskTemplatesWithFamilyByIdsSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplate> { template });

    _scheduleEvaluator.ShouldTriggerInWindow(template.Schedule, checkFrom, checkTo, family.Timezone)
      .Returns(triggerTime);

    _taskRepository.ListAsync(Arg.Any<TaskInstancesByTemplateSpec>(), Arg.Any<CancellationToken>())
      .Returns(existingInstances);

    IEnumerable<TaskInstance>? capturedExistingInstances = null;
    _taskInstanceFactory.CreateFromTemplate(
        Arg.Is(template),
        Arg.Any<DateTime>(),
        Arg.Do<IEnumerable<TaskInstance>>(instances => capturedExistingInstances = instances))
      .Returns(Result<TaskInstance>.Success(taskInstance));

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedExistingInstances.ShouldNotBeNull();
    capturedExistingInstances.ShouldBe(existingInstances);
  }
}
