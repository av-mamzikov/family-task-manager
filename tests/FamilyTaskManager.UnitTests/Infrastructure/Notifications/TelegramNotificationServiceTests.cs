using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Infrastructure.Notifications;
using FamilyTaskManager.TestInfrastructure;
using FamilyTaskManager.UseCases.Families.Specifications;
using FamilyTaskManager.UseCases.Tasks;
using User = FamilyTaskManager.Core.UserAggregate.User;

namespace FamilyTaskManager.UnitTests.Infrastructure.Notifications;

public class TelegramNotificationServiceTests
{
  private readonly TestTelegramBotClient _botClient;
  private readonly IRepository<Family> _familyRepository;
  private readonly ILogger<TelegramNotificationService> _logger;
  private readonly TelegramNotificationService _service;
  private readonly IRepository<User> _userRepository;

  public TelegramNotificationServiceTests()
  {
    _botClient = new TestTelegramBotClient();
    _familyRepository = Substitute.For<IRepository<Family>>();
    _userRepository = Substitute.For<IRepository<User>>();
    _logger = Substitute.For<ILogger<TelegramNotificationService>>();
    _service = new TelegramNotificationService(_botClient, _familyRepository, _userRepository, _logger);
  }

  [Fact]
  public async Task SendTaskReminderToFamilyAsync_WithValidFamily_SendsToAllActiveMembers()
  {
    // Arrange
    const long telegramId1 = 12345L;
    const long telegramId2 = 67890L;

    var family = new Family("Test Family", "Europe/Moscow");
    var user1 = new User(telegramId1, "User1");
    var user2 = new User(telegramId2, "User2");

    var member1 = family.AddMember(user1.Id, FamilyRole.Adult);
    var member2 = family.AddMember(user2.Id, FamilyRole.Child);
    family.ClearDomainEvents();

    var taskReminder = new TaskReminderDto(
      Guid.NewGuid(),
      family.Id,
      "Test Task",
      DateTime.UtcNow.AddHours(1),
      new List<Guid>());

    // Setup mocks
    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);
    _userRepository.GetByIdAsync(user1.Id, Arg.Any<CancellationToken>()).Returns(user1);
    _userRepository.GetByIdAsync(user2.Id, Arg.Any<CancellationToken>()).Returns(user2);

    // Act
    await _service.SendTaskReminderToFamilyAsync(family.Id, taskReminder, CancellationToken.None);

    // Assert
    _botClient.SentMessages.Count.ShouldBe(2);

    var message1 = _botClient.GetLastMessageTo(telegramId1);
    message1.ShouldNotBeNull();
    message1!.Text!.ShouldContain("Test Task");
    message1!.Text!.ShouldContain("Напоминание о задаче");

    var message2 = _botClient.GetLastMessageTo(telegramId2);
    message2.ShouldNotBeNull();
    message2!.Text!.ShouldContain("Test Task");
    message2!.Text!.ShouldContain("Напоминание о задаче");
  }

  [Fact]
  public async Task SendTaskCreatedAsync_WithValidParameters_SendsFormattedMessage()
  {
    // Arrange
    const long telegramId1 = 12345L;
    const long telegramId2 = 67890L;

    var family = new Family("Test Family", "Europe/Moscow");
    var user1 = new User(telegramId1, "User1");
    var user2 = new User(telegramId2, "User2");

    var member1 = family.AddMember(user1.Id, FamilyRole.Adult);
    var member2 = family.AddMember(user2.Id, FamilyRole.Child);
    family.ClearDomainEvents();

    var dueAt = DateTime.UtcNow.AddHours(1);

    // Setup mocks
    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);
    _userRepository.GetByIdAsync(user1.Id, Arg.Any<CancellationToken>()).Returns(user1);
    _userRepository.GetByIdAsync(user2.Id, Arg.Any<CancellationToken>()).Returns(user2);

    // Act
    await _service.SendTaskCreatedAsync(family.Id, "Test Task", new TaskPoints(2), "Fluffy", dueAt,
      CancellationToken.None);

    // Assert
    _botClient.SentMessages.Count.ShouldBe(2);

    var message1 = _botClient.GetLastMessageTo(telegramId1)!;
    message1.ShouldNotBeNull();
    message1.Text!.ShouldContain("Новая задача создана");
    message1.Text!.ShouldContain("Test Task");
    message1.Text!.ShouldContain("Fluffy");
    message1.Text!.ShouldContain("⭐⭐ очков");

    var message2 = _botClient.GetLastMessageTo(telegramId2)!;
    message2.ShouldNotBeNull();
    message2.Text!.ShouldContain("Новая задача создана");
    message2.Text!.ShouldContain("Test Task");
    message2.Text!.ShouldContain("Fluffy");
    message2.Text!.ShouldContain("⭐⭐ очков");
  }

  [Fact]
  public async Task SendTaskReminderToFamilyAsync_WithFamilyNotFound_LogsWarning()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var taskReminder = new TaskReminderDto(
      Guid.NewGuid(),
      familyId,
      "Test Task",
      DateTime.UtcNow.AddHours(1),
      new List<Guid>());

    // Setup mock to return null
    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns((Family?)null);

    // Act
    await _service.SendTaskReminderToFamilyAsync(familyId, taskReminder, CancellationToken.None);

    // Assert
    _botClient.SentMessages.Count.ShouldBe(0);
  }

  [Fact]
  public async Task SendTaskReminderToFamilyAsync_WithNoActiveMembers_LogsWarning()
  {
    // Arrange
    var family = new Family("Test Family", "Europe/Moscow");
    var user = new User(12345L, "InactiveUser");

    var inactiveMember = family.AddMember(user.Id, FamilyRole.Adult);
    family.ClearDomainEvents();

    var taskReminder = new TaskReminderDto(
      Guid.NewGuid(),
      family.Id,
      "Test Task",
      DateTime.UtcNow.AddHours(1),
      new List<Guid>());

    // Setup mock
    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    await _service.SendTaskReminderToFamilyAsync(family.Id, taskReminder, CancellationToken.None);

    // Assert
    _botClient.SentMessages.Count.ShouldBe(0);
  }

  [Fact]
  public async Task SendTaskReminderToFamilyAsync_WithUserNotFound_ContinuesWithOtherUsers()
  {
    // Arrange
    const long telegramId2 = 67890L;

    var family = new Family("Test Family", "Europe/Moscow");
    var user1 = new User(12345L, "User1");
    var user2 = new User(telegramId2, "User2");

    var member1 = family.AddMember(user1.Id, FamilyRole.Adult);
    var member2 = family.AddMember(user2.Id, FamilyRole.Child);
    family.ClearDomainEvents();

    var taskReminder = new TaskReminderDto(
      Guid.NewGuid(),
      family.Id,
      "Test Task",
      DateTime.UtcNow.AddHours(1),
      new List<Guid>());

    // Setup mocks - user1 not found, user2 found
    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);
    _userRepository.GetByIdAsync(user1.Id, Arg.Any<CancellationToken>()).Returns((User?)null);
    _userRepository.GetByIdAsync(user2.Id, Arg.Any<CancellationToken>()).Returns(user2);

    // Act
    await _service.SendTaskReminderToFamilyAsync(family.Id, taskReminder, CancellationToken.None);

    // Assert - should still send to user2
    await _userRepository.Received(1).GetByIdAsync(user1.Id, Arg.Any<CancellationToken>());
    await _userRepository.Received(1).GetByIdAsync(user2.Id, Arg.Any<CancellationToken>());

    _botClient.SentMessages.Count.ShouldBe(1);
    var message = _botClient.GetLastMessageTo(telegramId2)!;
    message.ShouldNotBeNull();
    message.Text!.ShouldContain("Test Task");
  }

  [Fact]
  public async Task SendTaskStartedAsync_WithValidParameters_SendsFormattedMessage()
  {
    // Arrange
    const long telegramId1 = 12345L;
    const long telegramId2 = 67890L;

    var family = new Family("Test Family", "Europe/Moscow");
    var user1 = new User(telegramId1, "User1");
    var user2 = new User(telegramId2, "User2");

    var member1 = family.AddMember(user1.Id, FamilyRole.Adult);
    var member2 = family.AddMember(user2.Id, FamilyRole.Child);
    family.ClearDomainEvents();

    // Setup mocks
    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);
    _userRepository.GetByIdAsync(user1.Id, Arg.Any<CancellationToken>()).Returns(user1);
    _userRepository.GetByIdAsync(user2.Id, Arg.Any<CancellationToken>()).Returns(user2);

    // Act
    await _service.SendTaskStartedAsync(family.Id, "User1", "Test Task", new TaskPoints(3),
      CancellationToken.None);

    // Assert
    _botClient.SentMessages.Count.ShouldBe(2);

    var message1 = _botClient.GetLastMessageTo(telegramId1)!;
    message1.ShouldNotBeNull();
    message1.Text!.ShouldContain("Задача взята в работу");
    message1.Text!.ShouldContain("User1");
    message1.Text!.ShouldContain("Test Task");
    message1.Text!.ShouldContain("⭐⭐⭐ очков");

    var message2 = _botClient.GetLastMessageTo(telegramId2)!;
    message2.ShouldNotBeNull();
    message2.Text!.ShouldContain("Задача взята в работу");
    message2.Text!.ShouldContain("User1");
    message2.Text!.ShouldContain("Test Task");
    message2.Text!.ShouldContain("⭐⭐⭐ очков");
  }
}
