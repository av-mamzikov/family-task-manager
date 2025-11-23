namespace FamilyTaskManager.UseCases.Pets;

/// <summary>
///   Defines default task templates for each pet type.
///   Based on the pet care requirements documented in docs/MVP1/Шаблоны питомцев и задач.md
/// </summary>
public static class PetTaskTemplateData
{
  private static readonly IReadOnlyList<TaskTemplateData> CatTemplates = new[]
  {
    // Ежедневные задачи
    new TaskTemplateData(
      "Убрать какахи из лотка кота",
      4,
      "0 0 9 * * ?" // каждый день в 9:00
    ),
    new TaskTemplateData(
      "Убрать комки из лотка кота",
      3,
      "0 0 20 * * ?" // каждый день в 20:00
    ),
    new TaskTemplateData(
      "Налить свежую воду коту",
      3,
      "0 0 9 * * ?" // каждый день в 9:00
    ),
    new TaskTemplateData(
      "Насыпать корм коту",
      3,
      "0 0 9 * * ?" // каждый день в 9:00
    ),

    // Периодические задачи
    new TaskTemplateData(
      "Полностью заменить наполнитель в лотке кота",
      7,
      "0 0 12 */7 * ?" // раз в 7 дней в 12:00
    ),
    new TaskTemplateData(
      "Помыть миски кота",
      4,
      "0 0 19 */2 * ?" // раз в 2 дня в 19:00
    ),
    new TaskTemplateData(
      "Почистить место для сна кота",
      4,
      "0 0 12 ? * SUN" // раз в неделю, в воскресенье в 12:00
    ),
    new TaskTemplateData(
      "Поиграть с котом",
      2,
      "0 0 18 * * ?" // каждый день в 18:00
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> DogTemplates = new[]
  {
    // Ежедневные задачи
    new TaskTemplateData(
      "Выгулять собаку утром",
      5,
      "0 0 8 * * ?" // каждый день в 8:00
    ),
    new TaskTemplateData(
      "Выгулять собаку вечером",
      5,
      "0 0 20 * * ?" // каждый день в 20:00
    ),
    new TaskTemplateData(
      "Накормить собаку",
      4,
      "0 0 9 * * ?" // каждый день в 9:00
    ),

    // Периодические задачи
    new TaskTemplateData(
      "Помыть миски собаки",
      4,
      "0 0 19 */2 * ?" // раз в 2 дня в 19:00
    ),
    new TaskTemplateData(
      "Расчесать собаку",
      4,
      "0 0 18 ? * SAT" // раз в неделю, в субботу в 18:00
    ),
    new TaskTemplateData(
      "Искупать собаку",
      8,
      "0 0 15 1 * ?" // раз в месяц, 1-го числа в 15:00
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> HamsterTemplates = new[]
  {
    // Ежедневные задачи
    new TaskTemplateData(
      "Насыпать корм хомяку",
      2,
      "0 0 9 * * ?" // каждый день в 9:00
    ),
    new TaskTemplateData(
      "Проверить и долить воду хомяку",
      2,
      "0 0 9 * * ?" // каждый день в 9:00
    ),

    // Периодические задачи
    new TaskTemplateData(
      "Убрать клетку хомяка",
      5,
      "0 0 12 ? * SAT" // раз в неделю, в субботу в 12:00
    ),
    new TaskTemplateData(
      "Полностью помыть клетку хомяка",
      7,
      "0 0 12 1 * ?" // раз в месяц, 1-го числа в 12:00
    ),
    new TaskTemplateData(
      "Проверить игрушки и колесо хомяка",
      3,
      "0 0 18 ? * SUN" // раз в неделю, в воскресенье в 18:00
    )
  };

  public static IReadOnlyList<TaskTemplateData> GetDefaultTemplates(PetType petType)
  {
    return petType switch
    {
      PetType.Cat => CatTemplates,
      PetType.Dog => DogTemplates,
      PetType.Hamster => HamsterTemplates,
      _ => throw new ArgumentOutOfRangeException(nameof(petType), petType, "Unknown pet type")
    };
  }

  public record TaskTemplateData(string Title, int Points, string Schedule);
}
