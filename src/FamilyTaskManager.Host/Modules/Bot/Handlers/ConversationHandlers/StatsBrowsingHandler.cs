using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Features.Statistics.Queries;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class StatsBrowsingHandler(
  ILogger<StatsBrowsingHandler> logger,
  IMediator mediator)
  : BaseConversationHandler(logger), IConversationHandler
{
  public Task HandleMessageAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken) => Task.CompletedTask;

  public async Task HandleCallbackAsync(ITelegramBotClient botClient,
    long chatId,
    Message? message,
    string[] callbackParts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (callbackParts.IsCallbackOf(CallbackData.Stats.List))
      await HandleListCallbackAsync(botClient, chatId, message, callbackParts, session, fromUser, cancellationToken);
  }

  private async Task HandleListCallbackAsync(ITelegramBotClient botClient, long chatId, Message? message,
    string[] callbackParts, UserSession session, User fromUser, CancellationToken cancellationToken)
  {
    // Get leaderboard
    var getLeaderboardQuery = new GetLeaderboardQuery(session.CurrentFamilyId!.Value);
    var leaderboardResult = await mediator.Send(getLeaderboardQuery, cancellationToken);

    var messageText = "📊 *Статистика семьи*\n\n";

    if (leaderboardResult.IsSuccess)
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

        var isCurrentUser = entry.UserId == session.UserId;
        var marker = isCurrentUser ? "➡️ " : "";

        messageText +=
          $"{marker}{medal} *{RoleDisplay.GetRoleCaption(entry.Role)} {entry.UserName}* - ⭐ {entry.Points}\n";
        position++;
      }
    }

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      messageText,
      ParseMode.Markdown,
      cancellationToken: cancellationToken);
  }
}
