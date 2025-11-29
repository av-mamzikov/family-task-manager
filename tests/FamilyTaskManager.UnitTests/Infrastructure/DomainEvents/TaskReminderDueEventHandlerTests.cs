using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.Infrastructure.DomainEvents;
using FamilyTaskManager.Infrastructure.Notifications;
using FamilyTaskManager.UseCases.Pets.Specifications;
using FamilyTaskManager.UseCases.Tasks;
using NSubstitute.ExceptionExtensions;

namespace FamilyTaskManager.UnitTests.Infrastructure.DomainEvents;

public class TaskReminderDueEventHandlerTests
{
  private readonly TaskReminderDueEventHandler _handler;
  private readonly ILogger<TaskReminderDueEventHandler> _logger;
  private readonly ITelegramNotificationService _notificationService;
  private readonly IRepository<Pet> _petRepository;
  private readonly ITimeZoneService _timeZoneService;

  public TaskReminderDueEventHandlerTests()
  {
    _logger = Substitute.For<ILogger<TaskReminderDueEventHandler>>();
    _notificationService = Substitute.For<ITelegramNotificationService>();
    _petRepository = Substitute.For<IRepository<Pet>>();
    _timeZoneService = Substitute.For<ITimeZoneService>();
    _handler = new TaskReminderDueEventHandler(_logger, _notificationService, _petRepository, _timeZoneService);
  }

  [Fact]
  public async Task Handle_WithValidTask_SendsReminderNotification()
  {
    // Arrange
    var familyTimezone = "Europe/Moscow";
    var dueAtUtc = DateTime.UtcNow.AddHours(1);
    var dueAtLocal = DateTime.UtcNow.AddHours(1); // Will be mocked

    var family = new Family("Test Family", familyTimezone);
    var pet = new Pet(family.Id, PetType.Dog, "Test Pet");

    // Use reflection to set Family navigation property
    typeof(Pet).GetProperty("Family")?.SetValue(pet, family);

    var task = new TaskInstance(
      family.Id,
      pet.Id,
      "Test Task",
      new TaskPoints(2),
      TaskType.OneTime,
      dueAtUtc);

    var domainEvent = new TaskReminderDueEvent(task);

    // Setup repository mock
    _petRepository.FirstOrDefaultAsync(Arg.Any<GetPetWithFamilySpec>(), Arg.Any<CancellationToken>())
      .Returns(pet);

    // Setup timezone service mock
    _timeZoneService.ConvertFromUtc(dueAtUtc, familyTimezone)
      .Returns(dueAtLocal);

    // Act
    await _handler.Handle(domainEvent, CancellationToken.None);

    // Assert
    await _notificationService.Received(1).SendTaskReminderToFamilyAsync(
      family.Id,
      Arg.Is<TaskReminderDto>(dto =>
        dto.TaskId == task.Id &&
        dto.FamilyId == family.Id &&
        dto.Title == "Test Task" &&
        dto.DueAt == dueAtLocal),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WithPetNotFound_LogsWarningAndDoesNotSendNotification()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var dueAtUtc = DateTime.UtcNow.AddHours(1);

    var task = new TaskInstance(
      familyId,
      petId,
      "Test Task",
      new TaskPoints(2),
      TaskType.OneTime,
      dueAtUtc);

    var domainEvent = new TaskReminderDueEvent(task);

    // Setup repository mock to return null
    _petRepository.FirstOrDefaultAsync(Arg.Any<GetPetWithFamilySpec>(), Arg.Any<CancellationToken>())
      .Returns((Pet?)null);

    // Act
    await _handler.Handle(domainEvent, CancellationToken.None);

    // Assert
    await _notificationService.DidNotReceive().SendTaskReminderToFamilyAsync(
      Arg.Any<Guid>(),
      Arg.Any<TaskReminderDto>(),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WithNotificationFailure_LogsErrorAndDoesNotThrow()
  {
    // Arrange
    var familyTimezone = "Europe/Moscow";
    var dueAtUtc = DateTime.UtcNow.AddHours(1);
    var dueAtLocal = DateTime.UtcNow.AddHours(1);

    var family = new Family("Test Family", familyTimezone);
    var pet = new Pet(family.Id, PetType.Dog, "Test Pet");

    // Use reflection to set Family navigation property
    typeof(Pet).GetProperty("Family")?.SetValue(pet, family);

    var task = new TaskInstance(
      family.Id,
      pet.Id,
      "Test Task",
      new TaskPoints(2),
      TaskType.OneTime,
      dueAtUtc);

    var domainEvent = new TaskReminderDueEvent(task);

    // Setup repository mock
    _petRepository.FirstOrDefaultAsync(Arg.Any<GetPetWithFamilySpec>(), Arg.Any<CancellationToken>())
      .Returns(pet);

    // Setup timezone service mock
    _timeZoneService.ConvertFromUtc(dueAtUtc, familyTimezone)
      .Returns(dueAtLocal);

    // Setup notification service to throw exception
    _notificationService.SendTaskReminderToFamilyAsync(
        Arg.Any<Guid>(),
        Arg.Any<TaskReminderDto>(),
        Arg.Any<CancellationToken>())
      .Throws(new Exception("Notification failed"));

    // Act & Assert - should not throw
    await _handler.Handle(domainEvent, CancellationToken.None);

    // Verify the method was called despite the exception
    await _notificationService.Received(1).SendTaskReminderToFamilyAsync(
      family.Id,
      Arg.Is<TaskReminderDto>(dto =>
        dto.TaskId == task.Id &&
        dto.FamilyId == family.Id &&
        dto.Title == "Test Task" &&
        dto.DueAt == dueAtLocal),
      Arg.Any<CancellationToken>());
  }
}
