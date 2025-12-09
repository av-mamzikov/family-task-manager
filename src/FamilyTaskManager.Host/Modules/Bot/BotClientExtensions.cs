using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot;

public static class BotClientExtensions
{
  public static async Task DeleteMessageIfCanAsync(this ITelegramBotClient botClient,
    ChatId chatId,
    Message? message,
    CancellationToken cancellationToken = default)
  {
    if (message == null)
      return;
    if (!await botClient.CanEditMessageAsync(message))
      return;
    await botClient.DeleteMessageAsync(chatId, message.MessageId!, cancellationToken);
  }

  public static async Task SendOrEditMessageAsync(this ITelegramBotClient botClient,
    long chatId,
    Message? message,
    string text,
    ParseMode? parseMode = null,
    IReplyMarkup? replyMarkup = null,
    CancellationToken cancellationToken = default)
  {
    if (message != null && await botClient.CanEditMessageAsync(message) &&
        replyMarkup is InlineKeyboardMarkup inlineKeyboard)
    {
      try
      {
        await botClient.EditMessageTextAsync(
          message.Chat.Id,
          message.MessageId,
          text,
          parseMode,
          replyMarkup: inlineKeyboard,
          cancellationToken: cancellationToken);
      }
      catch (ApiRequestException ex) when (
        ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
      {
        // Игнорируем: содержимое и клавиатура уже совпадают
      }
    }
    else
      await botClient.SendTextMessageAsync(
        chatId,
        text,
        parseMode: parseMode,
        replyMarkup: replyMarkup,
        cancellationToken: cancellationToken);
  }

  public static async Task<bool> CanEditMessageAsync(this ITelegramBotClient botClient, Message message)
  {
    if (message.From == null)
      return false;
    var botUser = await botClient.GetMeAsync();
    if (message.From.Id != botUser.Id)
      return false;
    if (message.Text == null && message.Caption == null)
      return false;
    var timeDiff = DateTime.UtcNow - message.Date;
    if (timeDiff.TotalHours >= 48)
      return false;
    return true;
  }
}
