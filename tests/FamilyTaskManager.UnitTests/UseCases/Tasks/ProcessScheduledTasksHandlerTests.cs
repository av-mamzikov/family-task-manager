using Ardalis.Result;
using Ardalis.Specification;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.Services;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.UseCases.Contracts.TaskTemplates;
using FamilyTaskManager.UseCases.Spots.Specifications;
using FamilyTaskManager.UseCases.Tasks;
using FamilyTaskManager.UseCases.Tasks.Specifications;
using FamilyTaskManager.UseCases.TaskTemplates.Specifications;

namespace FamilyTaskManager.UnitTests.UseCases.Tasks;

public class ProcessScheduledTasksHandlerTests
{
  private readonly ProcessScheduledTasksHandler _handler;
  private readonly ILogger<ProcessScheduledTasksHandler> _logger;
  private readonly IAppRepository<Spot> _SpotAppRepository;
  private readonly IAppRepository<TaskInstance> _taskAppRepository;
  private readonly ITaskInstanceFactory _taskInstanceFactory;
  private readonly IReadRepository<TaskTemplate> _templateRepository;

  public ProcessScheduledTasksHandlerTests()
  {
    _templateRepository = Substitute.For<IReadRepository<TaskTemplate>>();
    _taskAppRepository = Substitute.For<IAppRepository<TaskInstance>>();
    _SpotAppRepository = Substitute.For<IAppRepository<Spot>>();
    _taskInstanceFactory = Substitute.For<ITaskInstanceFactory>();
    _logger = Substitute.For<ILogger<ProcessScheduledTasksHandler>>();

    _handler = new(
      _templateRepository,
      _taskAppRepository,
      _SpotAppRepository,
      _taskInstanceFactory,
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
    var SpotId = Guid.NewGuid();
    var templateId = Guid.NewGuid();
    var checkFrom = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc); // 8:00 UTC
    var checkTo = checkFrom.AddHours(1); // 9:00 UTC

    var command = new ProcessScheduledTaskCommand(checkFrom, checkTo);

    // Schedule at 10:00 - outside the window
    var schedule = Schedule.CreateDaily(new(10, 0)).Value;
    var family = new Family("Test Family", "UTC", false);
    var template = new TaskTemplate(familyId, SpotId, "Daily Task", new(2), schedule, TimeSpan.FromHours(24),
      Guid.NewGuid());
    typeof(TaskTemplate).GetProperty(nameof(TaskTemplate.Family))!.SetValue(template, family);

    var templateDto = new TaskTemplateDto(
      templateId,
      familyId,
      "Daily Task",
      new(2),
      schedule.Type,
      schedule.Time,
      schedule.DayOfWeek,
      schedule.DayOfMonth,
      SpotId,
      "Test Spot",
      DateTime.UtcNow,
      TimeSpan.FromHours(24));

    _templateRepository.ListAsync(Arg.Any<ActiveTaskTemplatesWithTimeZoneSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplateDto> { templateDto });

    _templateRepository.ListAsync(Arg.Any<TaskTemplatesWithFamilyByIdsSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplate> { template });

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBe(0);
    await _taskAppRepository.DidNotReceive().AddAsync(Arg.Any<TaskInstance>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_TemplateTriggeredInWindow_CreatesInstance()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var SpotId = Guid.NewGuid();
    var templateId = Guid.NewGuid();
    var checkFrom = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc); // 9:00 UTC
    var checkTo = checkFrom.AddHours(2); // 11:00 UTC

    var command = new ProcessScheduledTaskCommand(checkFrom, checkTo);

    // Schedule at 10:00 - inside the window
    var schedule = Schedule.CreateDaily(new(10, 0)).Value;
    var family = new Family("Test Family", "UTC", false);
    var template = new TaskTemplate(familyId, SpotId, "Daily Task", new(2), schedule, TimeSpan.FromHours(24),
      Guid.NewGuid());
    typeof(TaskTemplate).GetProperty(nameof(TaskTemplate.Family))!.SetValue(template, family);

    var templateDto = new TaskTemplateDto(
      templateId,
      familyId,
      "Daily Task",
      new(2),
      schedule.Type,
      schedule.Time,
      schedule.DayOfWeek,
      schedule.DayOfMonth,
      SpotId,
      "Test Spot",
      DateTime.UtcNow,
      TimeSpan.FromHours(24));

    var Spot = new Spot(familyId, SpotType.Cat, "Test Spot");
    typeof(Spot).GetProperty("Family")!.SetValue(Spot, family);
    var newInstance = new TaskInstance(Spot, "Daily Task", new(2), DateTime.UtcNow.AddHours(24), templateId);

    _templateRepository.ListAsync(Arg.Any<ActiveTaskTemplatesWithTimeZoneSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplateDto> { templateDto });

    _templateRepository.ListAsync(Arg.Any<TaskTemplatesWithFamilyByIdsSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplate> { template });

    _SpotAppRepository.FirstOrDefaultAsync(Arg.Any<GetSpotByIdWithFamilySpec>(), Arg.Any<CancellationToken>())
      .Returns(Spot);

    _taskAppRepository.ListAsync(Arg.Any<ISpecification<TaskInstance>>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskInstance>());

    _taskInstanceFactory
      .CreateFromTemplate(template, Arg.Any<Spot>(), Arg.Any<DateTime>(), Arg.Any<IEnumerable<TaskInstance>>())
      .Returns(Result<TaskInstance>.Success(newInstance));

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBe(1);
    await _taskAppRepository.Received(1).AddAsync(Arg.Any<TaskInstance>(), Arg.Any<CancellationToken>());
    await _taskAppRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_FactoryReturnsError_DoesNotCreateInstance()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var SpotId = Guid.NewGuid();
    var templateId = Guid.NewGuid();
    var checkFrom = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);
    var checkTo = checkFrom.AddHours(2);

    var command = new ProcessScheduledTaskCommand(checkFrom, checkTo);

    var schedule = Schedule.CreateDaily(new(10, 0)).Value;
    var family = new Family("Test Family", "UTC", false);
    var template = new TaskTemplate(familyId, SpotId, "Daily Task", new(2), schedule, TimeSpan.FromHours(24),
      Guid.NewGuid());
    typeof(TaskTemplate).GetProperty(nameof(TaskTemplate.Family))!.SetValue(template, family);

    var templateDto = new TaskTemplateDto(
      templateId,
      familyId,
      "Daily Task",
      new(2),
      schedule.Type,
      schedule.Time,
      schedule.DayOfWeek,
      schedule.DayOfMonth,
      SpotId,
      "Test Spot",
      DateTime.UtcNow,
      TimeSpan.FromHours(24));

    var Spot = new Spot(familyId, SpotType.Cat, "Test Spot");
    typeof(Spot).GetProperty("Family")!.SetValue(Spot, family);

    _templateRepository.ListAsync(Arg.Any<ActiveTaskTemplatesWithTimeZoneSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplateDto> { templateDto });

    _templateRepository.ListAsync(Arg.Any<TaskTemplatesWithFamilyByIdsSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplate> { template });

    _SpotAppRepository.FirstOrDefaultAsync(Arg.Any<GetSpotByIdWithFamilySpec>(), Arg.Any<CancellationToken>())
      .Returns(Spot);

    _taskAppRepository.ListAsync(Arg.Any<ISpecification<TaskInstance>>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskInstance>());

    // Factory returns error (e.g., already has active instance)
    _taskInstanceFactory
      .CreateFromTemplate(template, Arg.Any<Spot>(), Arg.Any<DateTime>(), Arg.Any<IEnumerable<TaskInstance>>())
      .Returns(Result<TaskInstance>.Error("Already has active instance"));

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBe(0);
    await _taskAppRepository.DidNotReceive().AddAsync(Arg.Any<TaskInstance>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_MultipleTemplates_CreatesMultipleInstances()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var SpotId = Guid.NewGuid();
    var checkFrom = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);
    var checkTo = checkFrom.AddHours(2);

    var command = new ProcessScheduledTaskCommand(checkFrom, checkTo);

    var schedule1 = Schedule.CreateDaily(new(10, 0)).Value;
    var schedule2 = Schedule.CreateDaily(new(10, 30)).Value;

    var family = new Family("Test Family", "UTC", false);
    var Spot = new Spot(familyId, SpotType.Cat, "Test Spot");
    // Set Family navigation property for test
    typeof(Spot).GetProperty("Family")!.SetValue(Spot, family);

    var template1 = new TaskTemplate(familyId, SpotId, "Task 1", new(2), schedule1, TimeSpan.FromHours(24),
      Guid.NewGuid());
    typeof(TaskTemplate).GetProperty(nameof(TaskTemplate.Family))!.SetValue(template1, family);

    var template2 = new TaskTemplate(familyId, SpotId, "Task 2", new(2), schedule2, TimeSpan.FromHours(24),
      Guid.NewGuid());
    typeof(TaskTemplate).GetProperty(nameof(TaskTemplate.Family))!.SetValue(template2, family);

    var templateDtos = new List<TaskTemplateDto>
    {
      new(template1.Id, familyId, "Task 1", new(2), schedule1.Type, schedule1.Time, null, null, SpotId,
        "Spot",
        DateTime.UtcNow, TimeSpan.FromHours(24)),
      new(template2.Id, familyId, "Task 2", new(2), schedule2.Type, schedule2.Time, null, null, SpotId,
        "Spot",
        DateTime.UtcNow, TimeSpan.FromHours(24))
    };

    _templateRepository.ListAsync(Arg.Any<ActiveTaskTemplatesWithTimeZoneSpec>(), Arg.Any<CancellationToken>())
      .Returns(templateDtos);

    _templateRepository.ListAsync(Arg.Any<TaskTemplatesWithFamilyByIdsSpec>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskTemplate> { template1, template2 });

    _SpotAppRepository.FirstOrDefaultAsync(Arg.Any<GetSpotByIdWithFamilySpec>(), Arg.Any<CancellationToken>())
      .Returns(Spot);

    _taskAppRepository.ListAsync(Arg.Any<ISpecification<TaskInstance>>(), Arg.Any<CancellationToken>())
      .Returns(new List<TaskInstance>());

    _taskInstanceFactory.CreateFromTemplate(Arg.Any<TaskTemplate>(), Arg.Any<Spot>(), Arg.Any<DateTime>(),
        Arg.Any<IEnumerable<TaskInstance>>())
      .Returns(x =>
      {
        var template = x.ArgAt<TaskTemplate>(0);
        var Spot = x.ArgAt<Spot>(1);
        var dueAt = x.ArgAt<DateTime>(2);
        var instance = new TaskInstance(Spot, template.Title, template.Points, dueAt, template.Id);
        return Result<TaskInstance>.Success(instance);
      });

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBe(2);
    await _taskAppRepository.Received(2).AddAsync(Arg.Any<TaskInstance>(), Arg.Any<CancellationToken>());
    await _taskAppRepository.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
