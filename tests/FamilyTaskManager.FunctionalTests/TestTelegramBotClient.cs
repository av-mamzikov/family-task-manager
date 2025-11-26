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
  private readonly ConcurrentQueue<Update> _pendingUpdates = new();
  private readonly ConcurrentBag<string> _sentCallbackAnswers = new();
  private readonly ConcurrentBag<Message> _sentMessages = new();
  private readonly SemaphoreSlim _updateSignal = new(0);

  public Func<GetUpdatesRequest, Task<Update[]>>? GetUpdatesHandler { get; set; }

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
  public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);
  public IExceptionParser ExceptionsParser { get; set; } = null!;

  public Task<User> GetMeAsync(CancellationToken cancellationToken = default) =>
    Task.FromResult(BotUser);

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

  /// <summary>
  ///   Adds a single update to be returned by GetUpdatesRequest when no custom handler is set
  /// </summary>
  public void EnqueueUpdate(Update update)
  {
    _pendingUpdates.Enqueue(update);
    _updateSignal.Release();
  }

  /// <summary>
  ///   Adds multiple updates to be returned by GetUpdatesRequest when no custom handler is set
  /// </summary>
  public void EnqueueUpdates(IEnumerable<Update> updates)
  {
    foreach (var update in updates)
    {
      EnqueueUpdate(update);
    }
  }

  public async Task<TResponse> MakeRequestAsync<TResponse>(
    IRequest<TResponse> request,
    CancellationToken cancellationToken = default) =>
    request switch
    {
      SendMessageRequest sendRequest => (TResponse)(object)await HandleSendMessage(sendRequest),
      AnswerCallbackQueryRequest answerRequest => (TResponse)(object)await HandleAnswerCallback(answerRequest),
      EditMessageTextRequest editRequest => (TResponse)(object)await HandleEditMessage(editRequest),
      GetMeRequest getMeRequest => (TResponse)(object)await HandleGetMeMessage(getMeRequest),
      GetUpdatesRequest getUpdatesRequest => (TResponse)(object)await HandleGetUpdates(getUpdatesRequest),
      _ => throw new NotImplementedException($"Обработчик запроса {request} ещё не реализован")
    };

  private Task<User> HandleGetMeMessage(GetMeRequest _) =>
    Task.Run(() => BotUser);

  private Task<Update[]> HandleGetUpdates(GetUpdatesRequest _) =>
    Task.Run(async () =>
    {
      if (GetUpdatesHandler is not null)
      {
        return await GetUpdatesHandler(_);
      }

      var updates = new List<Update>();

      while (_pendingUpdates.TryDequeue(out var update))
      {
        updates.Add(update);
      }

      return updates.ToArray();
    });

  private Task<Message> HandleSendMessage(SendMessageRequest request) =>
    Task.Run(() =>
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
      return message;
    });

  private Task<bool> HandleAnswerCallback(AnswerCallbackQueryRequest request) =>
    Task.Run(() =>
    {
      if (!string.IsNullOrEmpty(request.Text))
      {
        _sentCallbackAnswers.Add(request.Text);
      }

      return true;
    });

  private Task<Message> HandleEditMessage(EditMessageTextRequest request) =>
    Task.Run(() =>
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

      return message;
    });

  /// <summary>
  ///   Simulates receiving updates. Runs a background loop and returns immediately
  ///   so it doesn't block host startup in tests.
  /// </summary>
  public Task ReceiveAsync(
    IUpdateHandler updateHandler,
    ReceiverOptions? receiverOptions = null,
    CancellationToken cancellationToken = default)
  {
    // Запускаем фоновой цикл обработки апдейтов и сразу возвращаем Task.CompletedTask,
    // чтобы TelegramBotHostedService не блокировал старт хоста.
    _ = Task.Run(async () =>
    {
      try
      {
        while (!cancellationToken.IsCancellationRequested)
        {
          // Сначала обрабатываем все накопившиеся апдейты
          while (_pendingUpdates.TryDequeue(out var update))
          {
            await updateHandler.HandleUpdateAsync(this, update, cancellationToken);
          }

          // Затем ждём, пока не придёт следующий апдейт или не будет отмена
          await _updateSignal.WaitAsync(cancellationToken);
        }
      }
      catch (OperationCanceledException)
      {
        // Ожидаемо при остановке хоста в тестах
      }
    }, cancellationToken);

    return Task.CompletedTask;
  }

#pragma warning disable CS0067 // Event is never used
  public event AsyncEventHandler<ApiRequestEventArgs>? OnMakingApiRequest;
  public event AsyncEventHandler<ApiResponseEventArgs>? OnApiResponseReceived;
#pragma warning restore CS0067
}
