using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Statistics;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;

public class StatsCommandHandler(IMediator mediator)
{
  public async Task HandleAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    Guid userId,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotConstants.Errors.NoFamily,
        cancellationToken: cancellationToken);
      return;
    }

    // Get leaderboard
    var getLeaderboardQuery = new GetLeaderboardQuery(session.CurrentFamilyId.Value);
    var leaderboardResult = await mediator.Send(getLeaderboardQuery, cancellationToken);

    var messageText = "📊 *Статистика семьи*\n\n";

    if (!leaderboardResult.IsSuccess)
    {
      // Leaderboard might be disabled
      messageText += BotConstants.Messages.LeaderboardDisabled;
    }
    else
    {
      var entries = leaderboardResult.Value;

      messageText += "*🏆 Лидерборд:*\n\n";

      var position = 1;
      foreach (var entry in entries)
      {
        var medal = position switch
        {
          1 => "🥇",
          2 => "🥈",
          3 => "🥉",
          _ => $"{position}."
        };

        var isCurrentUser = entry.UserId == userId;
        var marker = isCurrentUser ? "➡️ " : "";

        messageText += $"{marker}{medal} *{entry.UserName}* - ⭐ {entry.Points} очков\n";
        messageText += $"   Роль: {GetRoleText(entry.Role)}\n\n";

        position++;
      }
    }

    // Build inline keyboard
    var buttons = new List<InlineKeyboardButton[]>
    {
      new[]
      {
        InlineKeyboardButton.WithCallbackData("📜 История действий", $"stats_history_{session.CurrentFamilyId}")
      }
    };

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      messageText,
      parseMode: ParseMode.Markdown,
      replyMarkup: new InlineKeyboardMarkup(buttons),
      cancellationToken: cancellationToken);
  }

  private string GetRoleText(FamilyRole role)
  {
    return role switch
    {
      FamilyRole.Admin => "Администратор",
      FamilyRole.Adult => "Взрослый",
      FamilyRole.Child => "Ребёнок",
      _ => "Неизвестно"
    };
  }
}
