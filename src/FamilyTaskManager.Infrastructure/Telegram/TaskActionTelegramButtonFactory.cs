using FamilyTaskManager.Core.TaskAggregate;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Infrastructure.Telegram;

public static class TaskActionTelegramButtonFactory
{
  public static InlineKeyboardButton Create(TaskAction action, Guid taskId) =>
    Create(action, taskId, $"{GetEmoji(action)} {GetDefaultText(action)}");

  public static InlineKeyboardButton Create(TaskAction action, Guid taskId, string text) => action switch
  {
    TaskAction.Take => InlineKeyboardButton.WithCallbackData(text, CallbackData.TaskBrowsing.Take(taskId)),
    TaskAction.Complete => InlineKeyboardButton.WithCallbackData(text, CallbackData.TaskBrowsing.Complete(taskId)),
    TaskAction.Refuse => InlineKeyboardButton.WithCallbackData(text, CallbackData.TaskBrowsing.Refuse(taskId)),
    TaskAction.Delete => InlineKeyboardButton.WithCallbackData(text, CallbackData.TaskBrowsing.Delete(taskId)),
    _ => throw new InvalidOperationException($"Unsupported task action '{action}'")
  };

  private static string GetDefaultText(TaskAction action) => action switch
  {
    TaskAction.Take => "Начать",
    TaskAction.Complete => "Выполнить",
    TaskAction.Refuse => "Отказаться",
    TaskAction.Delete => "Удалить",
    _ => ""
  };

  public static string GetEmoji(TaskAction action) => action switch
  {
    TaskAction.Take => "✋",
    TaskAction.Complete => "✅",
    TaskAction.Refuse => "❌",
    TaskAction.Delete => "🗑️",
    _ => ""
  };
}
