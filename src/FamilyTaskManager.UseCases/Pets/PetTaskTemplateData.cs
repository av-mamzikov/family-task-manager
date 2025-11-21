namespace FamilyTaskManager.UseCases.Pets;

/// <summary>
/// Defines default task templates for each pet type.
/// Based on the pet care requirements documented in docs/MVP1/Шаблоны питомцев и задач.md
/// </summary>
public static class PetTaskTemplateData
{
  public record TaskTemplateData(string Title, int Points, string Schedule);

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

  private static readonly IReadOnlyList<TaskTemplateData> CatTemplates = new[]
  {
    // Ежедневные задачи
    new TaskTemplateData(
      Title: "Убрать какахи из лотка кота",
      Points: 4,
      Schedule: "0 0 9 * * ?"  // каждый день в 9:00
    ),
    new TaskTemplateData(
      Title: "Убрать комки из лотка кота",
      Points: 3,
      Schedule: "0 0 20 * * ?"  // каждый день в 20:00
    ),
    new TaskTemplateData(
      Title: "Налить свежую воду коту",
      Points: 3,
      Schedule: "0 0 9 * * ?"  // каждый день в 9:00
    ),
    new TaskTemplateData(
      Title: "Насыпать корм коту",
      Points: 3,
      Schedule: "0 0 9 * * ?"  // каждый день в 9:00
    ),
    
    // Периодические задачи
    new TaskTemplateData(
      Title: "Полностью заменить наполнитель в лотке кота",
      Points: 7,
      Schedule: "0 0 12 */7 * ?"  // раз в 7 дней в 12:00
    ),
    new TaskTemplateData(
      Title: "Помыть миски кота",
      Points: 4,
      Schedule: "0 0 19 */2 * ?"  // раз в 2 дня в 19:00
    ),
    new TaskTemplateData(
      Title: "Почистить место для сна кота",
      Points: 4,
      Schedule: "0 0 12 ? * SUN"  // раз в неделю, в воскресенье в 12:00
    ),
    new TaskTemplateData(
      Title: "Поиграть с котом",
      Points: 2,
      Schedule: "0 0 18 * * ?"  // каждый день в 18:00
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> DogTemplates = new[]
  {
    // Ежедневные задачи
    new TaskTemplateData(
      Title: "Выгулять собаку утром",
      Points: 5,
      Schedule: "0 0 8 * * ?"  // каждый день в 8:00
    ),
    new TaskTemplateData(
      Title: "Выгулять собаку вечером",
      Points: 5,
      Schedule: "0 0 20 * * ?"  // каждый день в 20:00
    ),
    new TaskTemplateData(
      Title: "Накормить собаку",
      Points: 4,
      Schedule: "0 0 9 * * ?"  // каждый день в 9:00
    ),
    
    // Периодические задачи
    new TaskTemplateData(
      Title: "Помыть миски собаки",
      Points: 4,
      Schedule: "0 0 19 */2 * ?"  // раз в 2 дня в 19:00
    ),
    new TaskTemplateData(
      Title: "Расчесать собаку",
      Points: 4,
      Schedule: "0 0 18 ? * SAT"  // раз в неделю, в субботу в 18:00
    ),
    new TaskTemplateData(
      Title: "Искупать собаку",
      Points: 8,
      Schedule: "0 0 15 1 * ?"  // раз в месяц, 1-го числа в 15:00
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> HamsterTemplates = new[]
  {
    // Ежедневные задачи
    new TaskTemplateData(
      Title: "Насыпать корм хомяку",
      Points: 2,
      Schedule: "0 0 9 * * ?"  // каждый день в 9:00
    ),
    new TaskTemplateData(
      Title: "Проверить и долить воду хомяку",
      Points: 2,
      Schedule: "0 0 9 * * ?"  // каждый день в 9:00
    ),
    
    // Периодические задачи
    new TaskTemplateData(
      Title: "Убрать клетку хомяка",
      Points: 5,
      Schedule: "0 0 12 ? * SAT"  // раз в неделю, в субботу в 12:00
    ),
    new TaskTemplateData(
      Title: "Полностью помыть клетку хомяка",
      Points: 7,
      Schedule: "0 0 12 1 * ?"  // раз в месяц, 1-го числа в 12:00
    ),
    new TaskTemplateData(
      Title: "Проверить игрушки и колесо хомяка",
      Points: 3,
      Schedule: "0 0 18 ? * SUN"  // раз в неделю, в воскресенье в 18:00
    )
  };
}
