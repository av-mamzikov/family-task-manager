using Shouldly;
using Xunit;

namespace FamilyTaskManager.FunctionalTests.Bot;

/// <summary>
/// Functional tests for bot commands.
/// Tests the full flow: Update → Handler → MediatR → Database → Response.
/// </summary>
public class BotCommandTests : BotFunctionalTestBase
{
  private const long TestChatId = 123456789;
  private const long TestUserId = 987654321;

  [Fact]
  public async Task Start_Command_Should_Send_Welcome_Message()
  {
    // Arrange - nothing needed, base class sets up everything
    
    // Act - simulate user sending /start command
    await SendCommandAsync(TestChatId, TestUserId, "/start");
    
    // Assert - verify bot sent a welcome message
    FakeBotClient.SentMessages.ShouldNotBeEmpty();
    var lastMessage = FakeBotClient.GetLastSentMessage();
    lastMessage.ShouldNotBeNull();
    lastMessage!.Text.ShouldNotBeNull();
    lastMessage.Text!.ShouldContain("Добро пожаловать");
  }

  [Fact]
  public async Task Help_Command_Should_Send_Help_Message()
  {
    // Act
    await SendCommandAsync(TestChatId, TestUserId, "/help");
    
    // Assert
    var lastMessage = FakeBotClient.GetLastSentMessage();
    lastMessage.ShouldNotBeNull();
    lastMessage!.Text.ShouldNotBeNull();
    lastMessage.Text!.ShouldContain("Доступные команды");
  }

  [Fact]
  public async Task Family_Command_Should_Show_Family_Options()
  {
    // Act
    await SendCommandAsync(TestChatId, TestUserId, "/family");
    
    // Assert
    var lastMessage = FakeBotClient.GetLastSentMessage();
    lastMessage.ShouldNotBeNull();
    lastMessage!.Text.ShouldNotBeNull();
    // Verify that message contains family-related content
    var text = lastMessage.Text!.ToLower();
    text.ShouldContain("сем");
  }

  [Fact]
  public async Task Tasks_Command_Should_Show_Tasks_List()
  {
    // Act
    await SendCommandAsync(TestChatId, TestUserId, "/tasks");
    
    // Assert
    var lastMessage = FakeBotClient.GetLastSentMessage();
    lastMessage.ShouldNotBeNull();
    lastMessage!.Text.ShouldNotBeNull();
    // Verify that message contains task-related content
    var text = lastMessage.Text!.ToLower();
    text.ShouldContain("задач");
  }

  [Fact]
  public async Task Unknown_Command_Should_Send_Error_Message()
  {
    // Act
    await SendCommandAsync(TestChatId, TestUserId, "/unknown");
    
    // Assert
    var lastMessage = FakeBotClient.GetLastSentMessage();
    lastMessage.ShouldNotBeNull();
    // Verify that bot responds to unknown commands
    lastMessage!.Text.ShouldNotBeNullOrEmpty();
  }

  [Fact]
  public async Task Multiple_Commands_Should_Be_Handled_Sequentially()
  {
    // Act - send multiple commands
    await SendCommandAsync(TestChatId, TestUserId, "/start");
    await SendCommandAsync(TestChatId, TestUserId, "/help");
    await SendCommandAsync(TestChatId, TestUserId, "/family");
    
    // Assert - verify all messages were sent
    FakeBotClient.SentMessages.Count.ShouldBeGreaterThanOrEqualTo(3);
  }
}
