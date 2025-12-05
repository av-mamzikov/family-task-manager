using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Families;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;

public class FamilyCommandHandler(IMediator mediator)
{
  public async Task HandleAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    Guid userId,
    CancellationToken cancellationToken)
  {
    // Get user families
    var getFamiliesQuery = new GetUserFamiliesQuery(userId);
    var familiesResult = await mediator.Send(getFamiliesQuery, cancellationToken);

    if (!familiesResult.IsSuccess || !familiesResult.Value.Any())
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Messages.NoFamilies,
        replyMarkup: new InlineKeyboardMarkup(new[]
        {
          InlineKeyboardButton.WithCallbackData("➕ Создать семью", CallbackData.Family.Create)
        }),
        cancellationToken: cancellationToken);
      return;
    }

    var families = familiesResult.Value;
    var currentFamilyId = session.CurrentFamilyId!.Value;

    // Build family list message
    var messageText = "🏠 *Ваши семьи:*\n\n";

    foreach (var family in families)
    {
      var isActive = family.Id == currentFamilyId;
      var marker = isActive ? "✅" : "⚪";
      messageText += $"{marker} *{family.Name}*\n";
      messageText += $"   Роль: {GetRoleEmoji(family.UserRole)} {GetRoleText(family.UserRole)}\n";
      messageText += $"   Очки: ⭐ {family.UserPoints}\n\n";
    }

    // Build inline keyboard
    var buttons = new List<InlineKeyboardButton[]>();

    foreach (var family in families)
      if (family.Id != currentFamilyId)
        buttons.Add(new[]
        {
          InlineKeyboardButton.WithCallbackData(
            $"Переключиться на \"{family.Name}\"",
            CallbackData.Family.Select(family.Id))
        });

    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("➕ Создать новую семью", CallbackData.Family.Create) });

    // Add admin actions for current family
    var currentFamily = families.FirstOrDefault(f => f.Id == currentFamilyId);
    if (currentFamily?.UserRole == FamilyRole.Admin)
    {
      buttons.Add(new[]
      {
        InlineKeyboardButton.WithCallbackData("👥 Управление участниками",
          CallbackData.Family.Members(currentFamilyId)),
        InlineKeyboardButton.WithCallbackData("🔗 Создать приглашение", CallbackData.Family.Invite(currentFamilyId))
      });
      buttons.Add(new[]
      {
        InlineKeyboardButton.WithCallbackData("⚙️ Настройки семьи", CallbackData.Family.Settings(currentFamilyId)),
        InlineKeyboardButton.WithCallbackData("🗑️ Удалить семью", CallbackData.Family.Delete(currentFamilyId))
      });
    }

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      messageText,
      parseMode: ParseMode.Markdown,
      replyMarkup: new InlineKeyboardMarkup(buttons),
      cancellationToken: cancellationToken);
  }

  private string GetRoleEmoji(FamilyRole role) =>
    role switch
    {
      FamilyRole.Admin => "👑",
      FamilyRole.Adult => "👤",
      FamilyRole.Child => "👶",
      _ => "❓"
    };

  private string GetRoleText(FamilyRole role) => BotMessages.Roles.GetRoleText(role);
}
