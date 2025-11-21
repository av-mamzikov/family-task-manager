using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using FamilyTaskManager.Bot.Models;
using FamilyTaskManager.UseCases.Statistics;
using FamilyTaskManager.Core.FamilyAggregate;
using Mediator;

namespace FamilyTaskManager.Bot.Handlers.Commands;

public class StatsCommandHandler
{
  private readonly IMediator _mediator;
  private readonly ILogger<StatsCommandHandler> _logger;

  public StatsCommandHandler(IMediator mediator, ILogger<StatsCommandHandler> logger)
  {
    _mediator = mediator;
    _logger = logger;
  }

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
        "❌ Сначала выберите активную семью через /family",
        cancellationToken: cancellationToken);
      return;
    }

    // Get leaderboard
    var getLeaderboardQuery = new GetLeaderboardQuery(session.CurrentFamilyId.Value);
    var leaderboardResult = await _mediator.Send(getLeaderboardQuery, cancellationToken);

    var messageText = "📊 *Статистика семьи*\n\n";

    if (!leaderboardResult.IsSuccess)
    {
      // Leaderboard might be disabled
      messageText += "Лидерборд отключён в настройках семьи.\n\n";
      messageText += "Администратор может включить его в настройках.";
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
      parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
      replyMarkup: new InlineKeyboardMarkup(buttons),
      cancellationToken: cancellationToken);
  }

  private string GetRoleText(FamilyRole role) => role switch
  {
    FamilyRole.Admin => "Администратор",
    FamilyRole.Adult => "Взрослый",
    FamilyRole.Child => "Ребёнок",
    _ => "Неизвестно"
  };
}
