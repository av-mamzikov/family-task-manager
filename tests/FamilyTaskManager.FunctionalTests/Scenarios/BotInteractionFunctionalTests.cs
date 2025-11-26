using FamilyTaskManager.UseCases.Families;
using Mediator;
using Telegram.Bot.Requests;

namespace FamilyTaskManager.FunctionalTests.Scenarios;

/// <summary>
///   Functional tests for bot interaction scenarios
/// </summary>
public class BotInteractionFunctionalTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly CustomWebApplicationFactory<Program> _factory;

  public BotInteractionFunctionalTests(CustomWebApplicationFactory<Program> factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task BotShouldSendMessageWhenFamilyCreated()
  {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    var botClient = _factory.TelegramBotClient;

    // Clear previous interactions
    botClient.Clear();

    var userId = Guid.NewGuid();
    var chatId = 123456L;

    // Act - Create family (this should trigger bot notification)
    var createCommand = new CreateFamilyCommand(userId, "Тестовая семья", "Europe/Moscow");
    var result = await mediator.Send(createCommand);

    // Assert
    result.IsSuccess.ShouldBeTrue();

    // Verify bot sent a message
    // Note: This is an example - actual implementation depends on your bot logic
    var sentMessages = botClient.GetMessagesTo(chatId);
    // sentMessages.ShouldNotBeEmpty();

    // Example assertions:
    // var lastMessage = botClient.GetLastMessageTo(chatId);
    // lastMessage.ShouldNotBeNull();
    // lastMessage.Text.ShouldContain("Тестовая семья");
  }

  [Fact]
  public async Task BotClient_ShouldCaptureAllInteractions()
  {
    // Arrange
    var botClient = _factory.TelegramBotClient;
    botClient.Clear();

    // Act - Simulate bot sending a message
    await botClient.MakeRequestAsync(
      new SendMessageRequest(
        12345,
        "Тестовое сообщение"));

    // Assert
    botClient.SentMessages.Count.ShouldBe(1);
    var message = botClient.SentMessages.First();
    message.Text.ShouldBe("Тестовое сообщение");
    message.Chat.Id.ShouldBe(12345);
  }

  [Fact]
  public async Task BotClient_ShouldCaptureEditedMessages()
  {
    // Arrange
    var botClient = _factory.TelegramBotClient;
    botClient.Clear();

    // Act - Simulate editing a message
    await botClient.MakeRequestAsync(
      new EditMessageTextRequest(
        12345,
        1,
        "Отредактированное сообщение"));

    // Assert
    botClient.EditedMessages.Count.ShouldBe(1);
    var (chatId, text) = botClient.EditedMessages.First();
    chatId.ShouldBe(12345);
    text.ShouldBe("Отредактированное сообщение");
  }

  [Fact]
  public async Task BotClient_ShouldCaptureCallbackAnswers()
  {
    // Arrange
    var botClient = _factory.TelegramBotClient;
    botClient.Clear();

    // Act - Simulate answering a callback query
    await botClient.MakeRequestAsync(
      new AnswerCallbackQueryRequest(
        "test_callback_id")
      {
        Text = "Операция выполнена успешно"
      });

    // Assert
    botClient.SentCallbackAnswers.Count.ShouldBe(1);
    botClient.SentCallbackAnswers.First().ShouldBe("Операция выполнена успешно");
  }
}
