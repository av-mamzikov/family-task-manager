using Ardalis.Result;
using FamilyTaskManager.Core.Services;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.UseCases.Tasks;
using FamilyTaskManager.UseCases.Tasks.Specifications;

namespace FamilyTaskManager.UnitTests.Host.Worker.UseCases;

public class CreateTaskInstanceFromTemplateTests
{
  private readonly CreateTaskInstanceFromTemplateHandler _handler;
  private readonly ITaskInstanceFactory _taskInstanceFactory;
  private readonly IRepository<TaskInstance> _taskRepository;
  private readonly IRepository<TaskTemplate> _templateRepository;

  public CreateTaskInstanceFromTemplateTests()
  {
    _templateRepository = Substitute.For<IRepository<TaskTemplate>>();
    _taskRepository = Substitute.For<IRepository<TaskInstance>>();
    _taskInstanceFactory = Substitute.For<ITaskInstanceFactory>();
    _handler = new CreateTaskInstanceFromTemplateHandler(_templateRepository, _taskRepository, _taskInstanceFactory);
  }

  [Fact]
  public async Task Handle_CreatesTaskInstance_WhenTemplateIsActive()
  {
    // Arrange
    var templateId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var dueAt = DateTime.UtcNow.AddHours(1);

    var template = new TaskTemplate(familyId, petId, "Test Task", 10, "0 0 9 * * ?", Guid.NewGuid());

    _templateRepository.GetByIdAsync(templateId, Arg.Any<CancellationToken>())
      .Returns(template);

    _taskRepository.ListAsync(Arg.Any<TaskInstancesByTemplateSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskInstance>()); // No existing instances

    var newInstance = new TaskInstance(
      familyId, petId, "Test Task", 10,
      TaskType.Recurring, dueAt, templateId);

    _taskInstanceFactory.CreateFromTemplate(template, dueAt, Arg.Any<List<TaskInstance>>())
      .Returns(Result.Success(newInstance));

    // Act
    var result = await _handler.Handle(
      new CreateTaskInstanceFromTemplateCommand(templateId, dueAt),
      CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    await _taskRepository.Received(1).AddAsync(
      Arg.Is<TaskInstance>(t =>
        t.TemplateId == templateId &&
        t.Title == "Test Task" &&
        t.Points == 10 &&
        t.Type == TaskType.Recurring &&
        t.DueAt == dueAt),
      Arg.Any<CancellationToken>());
    await _taskRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ReturnsNotFound_WhenTemplateDoesNotExist()
  {
    // Arrange
    var templateId = Guid.NewGuid();
    _templateRepository.GetByIdAsync(templateId, Arg.Any<CancellationToken>())
      .Returns((TaskTemplate?)null);

    // Act
    var result = await _handler.Handle(
      new CreateTaskInstanceFromTemplateCommand(templateId, DateTime.UtcNow),
      CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.NotFound);
    await _taskRepository.DidNotReceive().AddAsync(Arg.Any<TaskInstance>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ReturnsError_WhenTemplateIsNotActive()
  {
    // Arrange
    var templateId = Guid.NewGuid();
    var template = new TaskTemplate(
      Guid.NewGuid(), Guid.NewGuid(),
      "Test Task", 10, "0 0 9 * * ?", Guid.NewGuid());
    template.Deactivate();

    _templateRepository.GetByIdAsync(templateId, Arg.Any<CancellationToken>())
      .Returns(template);

    _taskRepository.ListAsync(Arg.Any<TaskInstancesByTemplateSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskInstance>());

    _taskInstanceFactory.CreateFromTemplate(template, Arg.Any<DateTime>(), Arg.Any<List<TaskInstance>>())
      .Returns(Result.Error("Template is not active"));

    // Act
    var result = await _handler.Handle(
      new CreateTaskInstanceFromTemplateCommand(templateId, DateTime.UtcNow),
      CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.Contains("not active"));
    await _taskRepository.DidNotReceive().AddAsync(Arg.Any<TaskInstance>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ReturnsError_WhenActiveInstanceAlreadyExists()
  {
    // Arrange - КРИТИЧНЫЙ ТЕСТ ДЛЯ ИНВАРИАНТА!
    var templateId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();

    var template = new TaskTemplate(familyId, petId, "Test Task", 10, "0 0 9 * * ?", Guid.NewGuid());

    // Существующий активный TaskInstance
    var existingInstance = new TaskInstance(
      familyId, petId, "Test Task", 10,
      TaskType.Recurring, DateTime.UtcNow, templateId);

    _templateRepository.GetByIdAsync(templateId, Arg.Any<CancellationToken>())
      .Returns(template);

    _taskRepository.ListAsync(Arg.Any<TaskInstancesByTemplateSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskInstance> { existingInstance });

    _taskInstanceFactory.CreateFromTemplate(template, Arg.Any<DateTime>(), Arg.Any<List<TaskInstance>>())
      .Returns(Result.Error("Active TaskInstance already exists"));

    // Act
    var result = await _handler.Handle(
      new CreateTaskInstanceFromTemplateCommand(templateId, DateTime.UtcNow),
      CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.Contains("Active TaskInstance already exists"));
    await _taskRepository.DidNotReceive().AddAsync(Arg.Any<TaskInstance>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_CreatesNewInstance_WhenPreviousInstanceIsCompleted()
  {
    // Arrange
    var templateId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();

    var template = new TaskTemplate(familyId, petId, "Test Task", 10, "0 0 9 * * ?", Guid.NewGuid());

    // Существующий ЗАВЕРШЕННЫЙ TaskInstance
    var completedInstance = new TaskInstance(
      familyId, petId, "Test Task", 10,
      TaskType.Recurring, DateTime.UtcNow.AddDays(-1), templateId);
    completedInstance.Complete(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1));

    _templateRepository.GetByIdAsync(templateId, Arg.Any<CancellationToken>())
      .Returns(template);

    _taskRepository.ListAsync(Arg.Any<TaskInstancesByTemplateSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskInstance> { completedInstance });

    var newInstance = new TaskInstance(
      familyId, petId, "Test Task", 10,
      TaskType.Recurring, DateTime.UtcNow, templateId);

    _taskInstanceFactory.CreateFromTemplate(template, Arg.Any<DateTime>(), Arg.Any<List<TaskInstance>>())
      .Returns(Result.Success(newInstance));

    // Act
    var result = await _handler.Handle(
      new CreateTaskInstanceFromTemplateCommand(templateId, DateTime.UtcNow),
      CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    await _taskRepository.Received(1).AddAsync(Arg.Any<TaskInstance>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_DoesNotCreateInstance_WhenPreviousInstanceIsInProgress()
  {
    // Arrange
    var templateId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();

    var template = new TaskTemplate(familyId, petId, "Test Task", 10, "0 0 9 * * ?", Guid.NewGuid());

    // Существующий TaskInstance "В работе"
    var inProgressInstance = new TaskInstance(
      familyId, petId, "Test Task", 10,
      TaskType.Recurring, DateTime.UtcNow.AddHours(-1), templateId);
    inProgressInstance.Start();

    _templateRepository.GetByIdAsync(templateId, Arg.Any<CancellationToken>())
      .Returns(template);

    _taskRepository.ListAsync(Arg.Any<TaskInstancesByTemplateSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskInstance> { inProgressInstance });

    _taskInstanceFactory.CreateFromTemplate(template, Arg.Any<DateTime>(), Arg.Any<List<TaskInstance>>())
      .Returns(Result.Error("Active TaskInstance already exists"));

    // Act
    var result = await _handler.Handle(
      new CreateTaskInstanceFromTemplateCommand(templateId, DateTime.UtcNow),
      CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.Contains("Active TaskInstance already exists"));
    await _taskRepository.DidNotReceive().AddAsync(Arg.Any<TaskInstance>(), Arg.Any<CancellationToken>());
  }
}
