using FamilyTaskManager.Core.FamilyAggregate.Events;
using Mediator;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Infrastructure.Notifications;

public class DailyOverdueTasksTelegramNotifier(
  ITelegramNotificationService telegramNotificationService)
  : INotificationHandler<DailyOverdueTasksSummaryDueEvent>
{
  public async ValueTask Handle(DailyOverdueTasksSummaryDueEvent notification, CancellationToken cancellationToken)
  {
    if (notification.OverdueTasksCount <= 0)
      return;

    var countText = notification.OverdueTasksCount == 1
      ? "1 задача требует внимания"
      : $"{notification.OverdueTasksCount} задач требуют внимания";

    var message = "⏰ *Ежедневное напоминание герою миссий!*\n" +
                  "(это сообщение видишь только ты)\n\n" +
                  $"{countText}.\n" +
                  "Пора действовать — загляни в свои миссии и закрой хвосты, чтобы семья гордилась тобой!";

    var replyMarkup = new InlineKeyboardMarkup([
      [InlineKeyboardButton.WithCallbackData("✅ Мои миссии", "Tasks_list")]
    ]);

    await telegramNotificationService.SendToUserAsync(
      notification.UserTelegramId,
      message,
      replyMarkup,
      cancellationToken);
  }
}
