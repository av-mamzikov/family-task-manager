using System.Text;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.UseCases.Families;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class FamilyMembersHandler(IMediator mediator)
{
  private readonly IMediator _mediator = mediator;

  public async Task ShowFamilyMembersAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid familyId,
    CancellationToken cancellationToken)
  {
    var result = await _mediator.Send(new GetFamilyMembersQuery(familyId), cancellationToken);
    if (!result.IsSuccess)
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        $"❌ Ошибка загрузки участников семьи: {result.Errors.FirstOrDefault()}",
        cancellationToken: cancellationToken);
      return;
    }

    var members = result.Value;
    var messageText = BuildMembersListText(members);
    var keyboard = BuildMembersKeyboard(familyId, members);

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      messageText,
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  public async Task ShowFamilyMemberAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid memberId,
    CancellationToken cancellationToken)
  {
    var member = await GetMemberAsync(memberId, cancellationToken);
    if (member == null)
    {
      await botClient.EditMessageTextAsync(chatId, messageId, "❌ Участник не найден",
        cancellationToken: cancellationToken);
      return;
    }

    var (roleEmoji, roleText) = GetRoleInfo(member.Role);
    var messageText = $"{roleEmoji} *{member.Name}*\n\n" +
                      $"Роль: {roleText}\n" +
                      $"Очки: ⭐ {member.Points}";

    var familyCode = CallbackDataHelper.EncodeGuid(member.FamilyId);
    var memberCode = CallbackDataHelper.EncodeGuid(member.Id);

    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[]
      {
        InlineKeyboardButton.WithCallbackData("♻️ Сменить роль", $"family_memberrole_{memberCode}"),
        InlineKeyboardButton.WithCallbackData("🗑️ Удалить участника", $"family_mdel_{memberCode}")
      },
      new[]
      {
        InlineKeyboardButton.WithCallbackData("⬅️ Назад к участникам", $"family_members_{familyCode}")
      }
    });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      messageText,
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  public async Task ShowRoleSelectionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid memberId,
    CancellationToken cancellationToken)
  {
    var member = await GetMemberAsync(memberId, cancellationToken);
    if (member == null)
    {
      await botClient.EditMessageTextAsync(chatId, messageId, "❌ Участник не найден",
        cancellationToken: cancellationToken);
      return;
    }

    var (roleEmoji, roleText) = GetRoleInfo(member.Role);

    var memberCode = CallbackDataHelper.EncodeGuid(member.Id);

    var availableRoles = Enum.GetValues<FamilyRole>()
      .Where(role => role != member.Role)
      .Select(role => new[]
      {
        InlineKeyboardButton.WithCallbackData(
          BotConstants.Roles.GetRoleText(role),
          $"family_mrpick_{memberCode}_{(int)role}")
      })
      .ToList();

    availableRoles.Add(new[]
    {
      InlineKeyboardButton.WithCallbackData(
        "⬅️ Назад",
        $"family_member_{memberCode}")
    });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      $"♻️ *Смена роли участника*\n\nТекущая роль: {roleEmoji} {roleText}. Выберите новую роль:",
      ParseMode.Markdown,
      replyMarkup: new InlineKeyboardMarkup(availableRoles),
      cancellationToken: cancellationToken);
  }

  public async Task ShowRemoveMemberConfirmationAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid memberId,
    CancellationToken cancellationToken)
  {
    var member = await GetMemberAsync(memberId, cancellationToken);
    if (member == null)
    {
      await botClient.EditMessageTextAsync(chatId, messageId, "❌ Участник не найден",
        cancellationToken: cancellationToken);
      return;
    }

    var (roleEmoji, roleText) = GetRoleInfo(member.Role);

    var memberCode = CallbackDataHelper.EncodeGuid(member.Id);

    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[]
      {
        InlineKeyboardButton.WithCallbackData(
          "✅ Да, удалить",
          $"family_mdelok_{memberCode}"),
        InlineKeyboardButton.WithCallbackData(
          "❌ Отмена",
          $"family_member_{memberCode}")
      }
    });

    var messageText = $"⚠️ *Удаление участника*\n\n" +
                      $"Вы уверены, что хотите удалить {roleEmoji} *{member.Name}* ({roleText}) из семьи?";

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      messageText,
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private async Task<FamilyMemberDto?> GetMemberAsync(Guid memberId, CancellationToken cancellationToken)
  {
    var result = await _mediator.Send(new GetFamilyMemberByIdQuery(memberId), cancellationToken);
    if (!result.IsSuccess)
      return null;
    return result.Value;
  }

  private static string BuildMembersListText(List<FamilyMemberDto> members)
  {
    if (!members.Any())
    {
      return "👥 *Участники семьи*\n\nВ этой семье пока нет активных участников.";
    }

    var sb = new StringBuilder("👥 *Участники семьи*\n\n");
    foreach (var member in members)
    {
      var (emoji, roleText) = GetRoleInfo(member.Role);
      sb.AppendLine($"{emoji} *{member.Name}*");
      sb.AppendLine($"   Роль: {roleText}");
      sb.AppendLine($"   Очки: ⭐ {member.Points}\n");
    }

    return sb.ToString();
  }

  private static InlineKeyboardMarkup BuildMembersKeyboard(Guid familyId, List<FamilyMemberDto> members)
  {
    var familyCode = CallbackDataHelper.EncodeGuid(familyId);
    var buttons = members.Select(member =>
    {
      var memberCode = CallbackDataHelper.EncodeGuid(member.Id);
      return new[]
      {
        InlineKeyboardButton.WithCallbackData(
          $"{GetRoleInfo(member.Role).emoji} {member.Name}",
          $"family_member_{memberCode}")
      };
    }).ToList();

    buttons.Add(new[]
    {
      InlineKeyboardButton.WithCallbackData(
        "🔗 Создать приглашение",
        $"family_invite_{familyCode}")
    });

    buttons.Add(new[]
    {
      InlineKeyboardButton.WithCallbackData(
        "⬅️ Назад",
        $"family_back_{familyCode}")
    });

    return new InlineKeyboardMarkup(buttons);
  }

  private static (string emoji, string text) GetRoleInfo(FamilyRole role) => role switch
  {
    FamilyRole.Admin => ("👑", "Администратор"),
    FamilyRole.Adult => ("👤", "Взрослый"),
    FamilyRole.Child => ("👶", "Ребёнок"),
    _ => ("❓", "Неизвестно")
  };
}
