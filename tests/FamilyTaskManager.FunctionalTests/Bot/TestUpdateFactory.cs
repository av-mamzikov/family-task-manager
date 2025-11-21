using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.FunctionalTests.Bot;

/// <summary>
/// Factory for creating test Update objects to simulate Telegram bot interactions.
/// </summary>
public static class TestUpdateFactory
{
  private static int _updateIdCounter = 1;
  private static int _messageIdCounter = 1;

  /// <summary>
  /// Creates a text message update from a user.
  /// </summary>
  public static Update CreateTextMessage(
    long chatId,
    long userId,
    string text,
    string? firstName = null,
    string? username = null)
  {
    return new Update
    {
      Id = _updateIdCounter++,
      Message = new Message
      {
        MessageId = _messageIdCounter++,
        Date = DateTime.UtcNow,
        Chat = new Chat
        {
          Id = chatId,
          Type = ChatType.Private,
          FirstName = firstName ?? "Test",
          Username = username
        },
        From = new User
        {
          Id = userId,
          IsBot = false,
          FirstName = firstName ?? "Test",
          Username = username
        },
        Text = text
      }
    };
  }

  /// <summary>
  /// Creates a callback query update from a button press.
  /// </summary>
  public static Update CreateCallbackQuery(
    long chatId,
    long userId,
    string callbackData,
    int messageId = 1,
    string? firstName = null,
    string? username = null)
  {
    return new Update
    {
      Id = _updateIdCounter++,
      CallbackQuery = new CallbackQuery
      {
        Id = Guid.NewGuid().ToString(),
        From = new User
        {
          Id = userId,
          IsBot = false,
          FirstName = firstName ?? "Test",
          Username = username
        },
        Message = new Message
        {
          MessageId = messageId,
          Date = DateTime.UtcNow,
          Chat = new Chat
          {
            Id = chatId,
            Type = ChatType.Private,
            FirstName = firstName ?? "Test",
            Username = username
          },
          Text = "Previous message"
        },
        Data = callbackData
      }
    };
  }

  /// <summary>
  /// Creates a /start command message.
  /// </summary>
  public static Update CreateStartCommand(
    long chatId,
    long userId,
    string? parameter = null,
    string? firstName = null,
    string? username = null)
  {
    var text = parameter != null ? $"/start {parameter}" : "/start";
    return CreateTextMessage(chatId, userId, text, firstName, username);
  }

  /// <summary>
  /// Creates a command message.
  /// </summary>
  public static Update CreateCommand(
    long chatId,
    long userId,
    string command,
    string? firstName = null,
    string? username = null)
  {
    var text = command.StartsWith("/") ? command : $"/{command}";
    return CreateTextMessage(chatId, userId, text, firstName, username);
  }

  /// <summary>
  /// Creates a message with inline keyboard.
  /// </summary>
  public static Message CreateMessageWithKeyboard(
    long chatId,
    string text,
    InlineKeyboardMarkup keyboard)
  {
    return new Message
    {
      MessageId = _messageIdCounter++,
      Date = DateTime.UtcNow,
      Chat = new Chat
      {
        Id = chatId,
        Type = ChatType.Private
      },
      Text = text,
      ReplyMarkup = keyboard
    };
  }

  /// <summary>
  /// Resets internal counters (useful for test isolation).
  /// </summary>
  public static void Reset()
  {
    _updateIdCounter = 1;
    _messageIdCounter = 1;
  }
}
