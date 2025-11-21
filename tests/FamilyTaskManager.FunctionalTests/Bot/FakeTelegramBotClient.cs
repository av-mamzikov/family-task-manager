using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FamilyTaskManager.FunctionalTests.Bot;

/// <summary>
/// Helper class for creating and configuring fake Telegram Bot Client for testing.
/// Uses NSubstitute to mock ITelegramBotClient and capture interactions.
/// </summary>
public class FakeTelegramBotClient
{
  private readonly ITelegramBotClient _botClient;
  private readonly List<Message> _sentMessages = new();
  private readonly List<(long ChatId, int MessageId, string Text)> _editedMessages = new();
  private readonly List<string> _answeredCallbacks = new();

  public ITelegramBotClient BotClient => _botClient;
  public IReadOnlyList<Message> SentMessages => _sentMessages.AsReadOnly();
  public IReadOnlyList<(long ChatId, int MessageId, string Text)> EditedMessages => _editedMessages.AsReadOnly();
  public IReadOnlyList<string> AnsweredCallbacks => _answeredCallbacks.AsReadOnly();

  public FakeTelegramBotClient()
  {
    _botClient = Substitute.For<ITelegramBotClient>();
    ConfigureBotClient();
  }

  private void ConfigureBotClient()
  {
    // Mock GetMeAsync
    _botClient.GetMeAsync(Arg.Any<CancellationToken>())
      .Returns(new User
      {
        Id = 123456789,
        IsBot = true,
        FirstName = "TestBot",
        Username = "test_bot"
      });

    // Mock SendTextMessageAsync
    _botClient.SendTextMessageAsync(
        Arg.Any<ChatId>(),
        Arg.Any<string>(),
        Arg.Any<int?>(),
        Arg.Any<Telegram.Bot.Types.Enums.ParseMode?>(),
        Arg.Any<IEnumerable<MessageEntity>?>(),
        Arg.Any<bool?>(),
        Arg.Any<bool?>(),
        Arg.Any<int?>(),
        Arg.Any<bool?>(),
        Arg.Any<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup?>(),
        Arg.Any<CancellationToken>())
      .Returns(callInfo =>
      {
        var chatId = callInfo.ArgAt<ChatId>(0);
        var text = callInfo.ArgAt<string>(1);
        
        var message = new Message
        {
          MessageId = _sentMessages.Count + 1,
          Date = DateTime.UtcNow,
          Chat = new Chat { Id = chatId.Identifier!.Value },
          Text = text
        };
        
        _sentMessages.Add(message);
        return message;
      });

    // Mock EditMessageTextAsync
    _botClient.EditMessageTextAsync(
        Arg.Any<ChatId>(),
        Arg.Any<int>(),
        Arg.Any<string>(),
        Arg.Any<Telegram.Bot.Types.Enums.ParseMode?>(),
        Arg.Any<IEnumerable<MessageEntity>?>(),
        Arg.Any<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup?>(),
        Arg.Any<CancellationToken>())
      .Returns(callInfo =>
      {
        var chatId = callInfo.ArgAt<ChatId>(0);
        var messageId = callInfo.ArgAt<int>(1);
        var text = callInfo.ArgAt<string>(2);
        
        _editedMessages.Add((chatId.Identifier!.Value, messageId, text));
        
        return new Message
        {
          MessageId = messageId,
          Date = DateTime.UtcNow,
          Chat = new Chat { Id = chatId.Identifier!.Value },
          Text = text
        };
      });

    // Mock AnswerCallbackQueryAsync
    _botClient.AnswerCallbackQueryAsync(
        Arg.Any<string>(),
        Arg.Any<string?>(),
        Arg.Any<bool?>(),
        Arg.Any<string?>(),
        Arg.Any<int?>(),
        Arg.Any<CancellationToken>())
      .Returns(callInfo =>
      {
        var callbackId = callInfo.ArgAt<string>(0);
        _answeredCallbacks.Add(callbackId);
        return true;
      });
  }

  public void Clear()
  {
    _sentMessages.Clear();
    _editedMessages.Clear();
    _answeredCallbacks.Clear();
  }

  public Message? GetLastSentMessage() => _sentMessages.LastOrDefault();
  
  public (long ChatId, int MessageId, string Text)? GetLastEditedMessage() => 
    _editedMessages.LastOrDefault();

  public bool WasCallbackAnswered(string callbackId) => 
    _answeredCallbacks.Contains(callbackId);
}
