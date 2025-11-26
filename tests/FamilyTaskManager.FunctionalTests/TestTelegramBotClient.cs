using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.FunctionalTests;

/// <summary>
///   Test implementation of ITelegramBotClient for functional tests.
///   Captures all bot interactions for verification in tests.
/// </summary>
public class TestTelegramBotClient : ITelegramBotClient
{
  private readonly ConcurrentBag<(long ChatId, string Text)> _editedMessages = new();
  private readonly ConcurrentBag<string> _sentCallbackAnswers = new();
  private readonly ConcurrentBag<Message> _sentMessages = new();

  public User BotUser { get; } = new()
  {
    Id = 123456789,
    IsBot = true,
    FirstName = "TestBot",
    Username = "test_bot"
  };

  /// <summary>
  ///   All messages sent by the bot during tests
  /// </summary>
  public IReadOnlyCollection<Message> SentMessages => _sentMessages.ToArray();

  /// <summary>
  ///   All callback query answers sent by the bot
  /// </summary>
  public IReadOnlyCollection<string> SentCallbackAnswers => _sentCallbackAnswers.ToArray();

  /// <summary>
  ///   All edited messages (ChatId, NewText)
  /// </summary>
  public IReadOnlyCollection<(long ChatId, string Text)> EditedMessages => _editedMessages.ToArray();

  Task<TResponse> ITelegramBotClient.MakeRequestAsync<TResponse>(
    IRequest<TResponse> request,
    CancellationToken cancellationToken) =>
    MakeRequestAsync(request, cancellationToken);

  public Task<bool> TestApiAsync(CancellationToken cancellationToken = default) =>
    Task.FromResult(true);

  public Task DownloadFileAsync(
    string filePath,
    Stream destination,
    CancellationToken cancellationToken = default) =>
    Task.CompletedTask;

  public bool LocalBotServer => false;
  public long? BotId => BotUser.Id;
  public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
  public IExceptionParser ExceptionsParser { get; set; } = null!;

  /// <summary>
  ///   Clears all captured interactions
  /// </summary>
  public void Clear()
  {
    _sentMessages.Clear();
    _sentCallbackAnswers.Clear();
    _editedMessages.Clear();
  }

  /// <summary>
  ///   Finds the last message sent to a specific chat
  /// </summary>
  public Message? GetLastMessageTo(long chatId) =>
    _sentMessages.LastOrDefault(m => m.Chat.Id == chatId);

  /// <summary>
  ///   Finds all messages sent to a specific chat
  /// </summary>
  public IEnumerable<Message> GetMessagesTo(long chatId) =>
    _sentMessages.Where(m => m.Chat.Id == chatId);

  public Task<TResponse> MakeRequestAsync<TResponse>(
    IRequest<TResponse> request,
    CancellationToken cancellationToken = default) =>
    request switch
    {
      SendMessageRequest sendRequest => HandleSendMessage(sendRequest) as Task<TResponse>,
      AnswerCallbackQueryRequest answerRequest => HandleAnswerCallback(answerRequest) as Task<TResponse>,
      EditMessageTextRequest editRequest => HandleEditMessage(editRequest) as Task<TResponse>,
      _ => Task.FromResult<TResponse>(default!)
    } ?? Task.FromResult<TResponse>(default!);

  private Task<Message> HandleSendMessage(SendMessageRequest request)
  {
    var message = new Message
    {
      MessageId = _sentMessages.Count + 1,
      Date = DateTime.UtcNow,
      Chat = new Chat { Id = request.ChatId.Identifier ?? 0, Type = ChatType.Private },
      Text = request.Text,
      ReplyMarkup = request.ReplyMarkup as InlineKeyboardMarkup
    };

    _sentMessages.Add(message);
    return Task.FromResult(message);
  }

  private Task<bool> HandleAnswerCallback(AnswerCallbackQueryRequest request)
  {
    if (!string.IsNullOrEmpty(request.Text))
    {
      _sentCallbackAnswers.Add(request.Text);
    }

    return Task.FromResult(true);
  }

  private Task<Message> HandleEditMessage(EditMessageTextRequest request)
  {
    _editedMessages.Add((request.ChatId.Identifier ?? 0, request.Text));

    var message = new Message
    {
      MessageId = request.MessageId,
      Date = DateTime.UtcNow,
      Chat = new Chat { Id = request.ChatId.Identifier ?? 0, Type = ChatType.Private },
      Text = request.Text,
      ReplyMarkup = request.ReplyMarkup
    };

    return Task.FromResult(message);
  }

  public Task<User> GetMeAsync(CancellationToken cancellationToken = default) =>
    Task.FromResult(BotUser);

  /// <summary>
  ///   Simulates receiving updates. In tests, this just waits for cancellation.
  /// </summary>
  public async Task ReceiveAsync(
    IUpdateHandler updateHandler,
    ReceiverOptions? receiverOptions = null,
    CancellationToken cancellationToken = default)
  {
    // In test mode, just wait for cancellation without actually polling
    try
    {
      await Task.Delay(System.Threading.Timeout.Infinite, cancellationToken);
    }
    catch (OperationCanceledException)
    {
      // Expected when the test host shuts down
    }
  }

#pragma warning disable CS0067 // Event is never used
  public event AsyncEventHandler<ApiRequestEventArgs>? OnMakingApiRequest;
  public event AsyncEventHandler<ApiResponseEventArgs>? OnApiResponseReceived;
#pragma warning restore CS0067
}
