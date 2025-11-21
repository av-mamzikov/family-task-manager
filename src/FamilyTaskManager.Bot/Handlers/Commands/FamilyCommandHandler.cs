using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using FamilyTaskManager.Bot.Services;
using FamilyTaskManager.Bot.Models;
using FamilyTaskManager.UseCases.Families;
using FamilyTaskManager.UseCases.Users;
using FamilyTaskManager.Core.FamilyAggregate;
using Mediator;

namespace FamilyTaskManager.Bot.Handlers.Commands;

public class FamilyCommandHandler
{
  private readonly IMediator _mediator;
  private readonly ILogger<FamilyCommandHandler> _logger;

  public FamilyCommandHandler(IMediator mediator, ILogger<FamilyCommandHandler> logger)
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
    // Get user families
    var getFamiliesQuery = new GetUserFamiliesQuery(userId);
    var familiesResult = await _mediator.Send(getFamiliesQuery, cancellationToken);

    if (!familiesResult.IsSuccess || !familiesResult.Value.Any())
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "У вас пока нет семей. Создайте свою первую семью!",
        replyMarkup: new InlineKeyboardMarkup(new[]
        {
          InlineKeyboardButton.WithCallbackData("➕ Создать семью", "create_family")
        }),
        cancellationToken: cancellationToken);
      return;
    }

    var families = familiesResult.Value;
    var currentFamilyId = session.CurrentFamilyId;

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
    {
      if (family.Id != currentFamilyId)
      {
        buttons.Add(new[]
        {
          InlineKeyboardButton.WithCallbackData(
            $"Переключиться на \"{family.Name}\"",
            $"select_family_{family.Id}")
        });
      }
    }

    buttons.Add(new[]
    {
      InlineKeyboardButton.WithCallbackData("➕ Создать новую семью", "create_family")
    });

    // Add admin actions for current family
    var currentFamily = families.FirstOrDefault(f => f.Id == currentFamilyId);
    if (currentFamily?.UserRole == FamilyRole.Admin)
    {
      buttons.Add(new[]
      {
        InlineKeyboardButton.WithCallbackData("👥 Управление участниками", $"family_members_{currentFamilyId}"),
        InlineKeyboardButton.WithCallbackData("🔗 Создать приглашение", $"family_invite_{currentFamilyId}")
      });
      buttons.Add(new[]
      {
        InlineKeyboardButton.WithCallbackData("⚙️ Настройки семьи", $"family_settings_{currentFamilyId}")
      });
    }

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      messageText,
      parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
      replyMarkup: new InlineKeyboardMarkup(buttons),
      cancellationToken: cancellationToken);
  }

  private string GetRoleEmoji(FamilyRole role) => role switch
  {
    FamilyRole.Admin => "👑",
    FamilyRole.Adult => "👤",
    FamilyRole.Child => "👶",
    _ => "❓"
  };

  private string GetRoleText(FamilyRole role) => role switch
  {
    FamilyRole.Admin => "Администратор",
    FamilyRole.Adult => "Взрослый",
    FamilyRole.Child => "Ребёнок",
    _ => "Неизвестно"
  };
}
