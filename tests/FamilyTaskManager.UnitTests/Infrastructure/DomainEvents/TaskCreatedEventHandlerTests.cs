using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.Infrastructure.DomainEvents;
using FamilyTaskManager.Infrastructure.Notifications;
using FamilyTaskManager.UseCases.Pets.Specifications;
using NSubstitute.ExceptionExtensions;

namespace FamilyTaskManager.UnitTests.Infrastructure.DomainEvents;

public class TaskCreatedEventHandlerTests
{
  private readonly TaskCreatedEventHandler _handler;
  private readonly ILogger<TaskCreatedEventHandler> _logger;
  private readonly ITelegramNotificationService _notificationService;
  private readonly IRepository<Pet> _petRepository;
  private readonly ITimeZoneService _timeZoneService;

  public TaskCreatedEventHandlerTests()
  {
    _logger = Substitute.For<ILogger<TaskCreatedEventHandler>>();
    _notificationService = Substitute.For<ITelegramNotificationService>();
    _petRepository = Substitute.For<IRepository<Pet>>();
    _timeZoneService = Substitute.For<ITimeZoneService>();
    _handler = new TaskCreatedEventHandler(_logger, _notificationService, _petRepository, _timeZoneService);
  }

  [Fact]
  public async Task Handle_WithValidTask_SendsNotification()
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

    var domainEvent = new TaskCreatedEvent(task);

    // Setup repository mock
    _petRepository.FirstOrDefaultAsync(Arg.Any<GetPetWithFamilySpec>(), Arg.Any<CancellationToken>())
      .Returns(pet);

    // Setup timezone service mock
    _timeZoneService.ConvertFromUtc(dueAtUtc, familyTimezone)
      .Returns(dueAtLocal);

    // Act
    await _handler.Handle(domainEvent, CancellationToken.None);

    // Assert
    await _notificationService.Received(1).SendTaskCreatedAsync(
      family.Id,
      "Test Task",
      2,
      "Test Pet",
      dueAtLocal,
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

    var domainEvent = new TaskCreatedEvent(task);

    // Setup repository mock to return null
    _petRepository.FirstOrDefaultAsync(Arg.Any<GetPetWithFamilySpec>(), Arg.Any<CancellationToken>())
      .Returns((Pet?)null);

    // Act
    await _handler.Handle(domainEvent, CancellationToken.None);

    // Assert
    await _notificationService.DidNotReceive().SendTaskCreatedAsync(
      Arg.Any<Guid>(),
      Arg.Any<string>(),
      Arg.Any<int>(),
      Arg.Any<string>(),
      Arg.Any<DateTime>(),
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

    var domainEvent = new TaskCreatedEvent(task);

    // Setup repository mock
    _petRepository.FirstOrDefaultAsync(Arg.Any<GetPetWithFamilySpec>(), Arg.Any<CancellationToken>())
      .Returns(pet);

    // Setup timezone service mock
    _timeZoneService.ConvertFromUtc(dueAtUtc, familyTimezone)
      .Returns(dueAtLocal);

    // Setup notification service to throw exception
    _notificationService.SendTaskCreatedAsync(
        Arg.Any<Guid>(),
        Arg.Any<string>(),
        Arg.Any<int>(),
        Arg.Any<string>(),
        Arg.Any<DateTime>(),
        Arg.Any<CancellationToken>())
      .Throws(new Exception("Notification failed"));

    // Act & Assert - should not throw
    await _handler.Handle(domainEvent, CancellationToken.None);

    // Verify the method was called despite the exception
    await _notificationService.Received(1).SendTaskCreatedAsync(
      family.Id,
      "Test Task",
      2,
      "Test Pet",
      dueAtLocal,
      Arg.Any<CancellationToken>());
  }
}
