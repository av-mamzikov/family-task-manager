using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FamilyTaskManager.FunctionalTests.Helpers;

/// <summary>
///   Factory for creating Telegram Update objects for bot flow tests
/// </summary>
public static class UpdateFactory
{
  /// <summary>
  ///   Create a text message update
  /// </summary>
  public static Update CreateTextUpdate(long chatId, long userId, string text, string? userName = null,
    string? firstName = null, string? lastName = null) =>
    new()
    {
      Id = Random.Shared.Next(1, int.MaxValue),
      Message = new Message
      {
        MessageId = Random.Shared.Next(1, int.MaxValue),
        Chat = new Chat { Id = chatId, Type = ChatType.Private },
        From = CreateUser(userId, userName, firstName, lastName),
        Text = text,
        Date = DateTime.UtcNow
      }
    };

  private static User CreateUser(long userId, string? userName = null, string? firstName = null,
    string? lastName = null) => new()
  {
    Id = userId,
    Username = userName ?? $"user{userId}",
    FirstName = firstName ?? $"firstName{userId}",
    LastName = lastName ?? $"lastName{userId}",
    IsBot = false
  };

  /// <summary>
  ///   Create a callback query update (inline button press)
  /// </summary>
  public static Update CreateCallbackUpdate(
    long chatId,
    long userId,
    string callbackData,
    int messageId = 1,
    string? username = null,
    string? firstName = null,
    string? lastName = null) =>
    new()
    {
      Id = Random.Shared.Next(1, int.MaxValue),
      CallbackQuery = new CallbackQuery
      {
        Id = Guid.NewGuid().ToString(),
        From = CreateUser(userId, username, firstName, lastName),
        Message = new Message
        {
          MessageId = messageId,
          Chat = new Chat { Id = chatId, Type = ChatType.Private },
          Date = DateTime.UtcNow
        },
        Data = callbackData,
        ChatInstance = Guid.NewGuid().ToString()
      }
    };

  /// <summary>
  ///   Create a location update (geolocation sharing)
  /// </summary>
  public static Update CreateLocationUpdate(
    long chatId,
    long userId,
    double latitude,
    double longitude,
    string? username = null,
    string? firstName = null,
    string? lastName = null) =>
    new()
    {
      Id = Random.Shared.Next(1, int.MaxValue),
      Message = new Message
      {
        MessageId = Random.Shared.Next(1, int.MaxValue),
        Chat = new Chat { Id = chatId, Type = ChatType.Private },
        From = CreateUser(userId, username, firstName, lastName),
        Location = new Location { Latitude = latitude, Longitude = longitude },
        Date = DateTime.UtcNow
      }
    };

  /// <summary>
  ///   Create a contact update (phone number sharing)
  /// </summary>
  public static Update CreateContactUpdate(
    long chatId,
    long userId,
    string phoneNumber,
    string firstName,
    string? username = null,
    string? lastName = null) =>
    new()
    {
      Id = Random.Shared.Next(1, int.MaxValue),
      Message = new Message
      {
        MessageId = Random.Shared.Next(1, int.MaxValue),
        Chat = new Chat { Id = chatId, Type = ChatType.Private },
        From = CreateUser(userId, username, firstName, lastName),
        Contact = new Contact
        {
          PhoneNumber = phoneNumber,
          FirstName = firstName,
          UserId = userId
        },
        Date = DateTime.UtcNow
      }
    };
}
