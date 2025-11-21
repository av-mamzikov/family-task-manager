using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Pets;
using FamilyTaskManager.Core.PetAggregate;
using Mediator;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;

public class PetCommandHandler(IMediator mediator)
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
        "❌ Сначала выберите активную семью через /family",
        cancellationToken: cancellationToken);
      return;
    }

    // Get pets
    var getPetsQuery = new GetPetsQuery(session.CurrentFamilyId.Value);
    var petsResult = await mediator.Send(getPetsQuery, cancellationToken);

    if (!petsResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка загрузки питомцев",
        cancellationToken: cancellationToken);
      return;
    }

    var pets = petsResult.Value;

    if (!pets.Any())
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "🐾 У вас пока нет питомцев.\n\nАдминистратор может создать питомца.",
        replyMarkup: new InlineKeyboardMarkup(new[]
        {
          InlineKeyboardButton.WithCallbackData("➕ Создать питомца", "create_pet")
        }),
        cancellationToken: cancellationToken);
      return;
    }

    var messageText = "🐾 *Ваши питомцы:*\n\n";

    foreach (var pet in pets)
    {
      var petEmoji = GetPetEmoji(pet.Type);
      var moodEmoji = GetMoodEmoji(pet.MoodScore);
      var moodText = GetMoodText(pet.MoodScore);

      messageText += $"{petEmoji} *{pet.Name}*\n";
      messageText += $"   Настроение: {moodEmoji} {pet.MoodScore}/100 - {moodText}\n";
      messageText += $"   Тип: {GetPetTypeText(pet.Type)}\n\n";
    }

    // Build inline keyboard
    var buttons = new List<InlineKeyboardButton[]>
    {
      new[]
      {
        InlineKeyboardButton.WithCallbackData("➕ Создать питомца", "create_pet")
      }
    };

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      messageText,
      parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
      replyMarkup: new InlineKeyboardMarkup(buttons),
      cancellationToken: cancellationToken);
  }

  private string GetPetEmoji(PetType type) => type switch
  {
    PetType.Cat => "🐱",
    PetType.Dog => "🐶",
    PetType.Hamster => "🐹",
    _ => "🐾"
  };

  private string GetPetTypeText(PetType type) => type switch
  {
    PetType.Cat => "Кот",
    PetType.Dog => "Собака",
    PetType.Hamster => "Хомяк",
    _ => "Неизвестно"
  };

  private string GetMoodEmoji(int moodScore) => moodScore switch
  {
    >= 80 => "😊",
    >= 60 => "🙂",
    >= 40 => "😐",
    >= 20 => "😟",
    _ => "😢"
  };

  private string GetMoodText(int moodScore) => moodScore switch
  {
    >= 80 => "Отлично!",
    >= 60 => "Хорошо",
    >= 40 => "Нормально",
    >= 20 => "Грустит",
    _ => "Очень грустно"
  };
}
