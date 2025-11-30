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
      3,
      Schedule.CreateDaily(new TimeOnly(5, 30)).Value, // каждый день в 5:30, до подъёма семьи
      TimeSpan.FromHours(2)
    ),
    new TaskTemplateData(
      "Убрать комки из лотка кота",
      2,
      Schedule.CreateDaily(new TimeOnly(20, 0)).Value, // каждый день в 20:00, после возвращения с работы
      TimeSpan.FromHours(2)
    ),
    new TaskTemplateData(
      "Налить свежую воду коту",
      2,
      Schedule.CreateDaily(new TimeOnly(6, 0)).Value, // каждый день в 6:00, сразу после подъёта
      TimeSpan.FromHours(2)
    ),
    new TaskTemplateData(
      "Насыпать корм коту",
      2,
      Schedule.CreateDaily(new TimeOnly(6, 0)).Value, // каждый день в 6:00, сразу после подъёта
      TimeSpan.FromHours(2)
    ),

    // Периодические задачи
    new TaskTemplateData(
      "Полностью заменить наполнитель в лотке кота",
      3,
      Schedule.CreateWeekly(new TimeOnly(12, 0), DayOfWeek.Sunday).Value, // раз в неделю в 12:00
      TimeSpan.FromHours(10)
    ),
    new TaskTemplateData(
      "Помыть миски кота",
      2,
      Schedule.CreateWeekly(new TimeOnly(19, 0), DayOfWeek.Monday)
        .Value, // 2 раза в неделю (используем понедельник и четверг)
      TimeSpan.FromHours(3)
    ),
    new TaskTemplateData(
      "Почистить место для сна кота",
      2,
      Schedule.CreateWeekly(new TimeOnly(12, 0), DayOfWeek.Sunday).Value, // раз в неделю, в воскресенье в 12:00
      TimeSpan.FromHours(10)
    ),
    new TaskTemplateData(
      "Поиграть с котом",
      1,
      Schedule.CreateDaily(new TimeOnly(19, 0)).Value, // каждый день в 19:00, после возвращения с работы
      TimeSpan.FromHours(3)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> DogTemplates = new[]
  {
    // Ежедневные задачи
    new TaskTemplateData(
      "Выгулять собаку утром",
      3,
      Schedule.CreateDaily(new TimeOnly(6, 0)).Value, // каждый день в 6:00, сразу после подъёта
      TimeSpan.FromHours(1.5)
    ),
    new TaskTemplateData(
      "Выгулять собаку вечером",
      3,
      Schedule.CreateDaily(new TimeOnly(19, 0)).Value, // каждый день в 19:00, сразу после возвращения с работы
      TimeSpan.FromHours(3)
    ),
    new TaskTemplateData(
      "Накормить собаку",
      2,
      Schedule.CreateDaily(new TimeOnly(6, 0)).Value, // каждый день в 6:00, сразу после подъёта
      TimeSpan.FromHours(2)
    ),

    // Периодические задачи
    new TaskTemplateData(
      "Помыть миски собаки",
      2,
      Schedule.CreateWeekly(new TimeOnly(19, 0), DayOfWeek.Monday).Value, // 2 раза в неделю
      TimeSpan.FromHours(3)
    ),
    new TaskTemplateData(
      "Расчесать собаку",
      2,
      Schedule.CreateWeekly(new TimeOnly(19, 0), DayOfWeek.Saturday).Value, // раз в неделю, в субботу в 19:00
      TimeSpan.FromHours(3)
    ),
    new TaskTemplateData(
      "Искупать собаку",
      3,
      Schedule.CreateMonthly(new TimeOnly(12, 0), 1).Value, // раз в месяц, 1-го числа в 12:00
      TimeSpan.FromHours(10)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> HamsterTemplates = new[]
  {
    // Ежедневные задачи
    new TaskTemplateData(
      "Насыпать корм хомяку",
      2,
      Schedule.CreateDaily(new TimeOnly(6, 0)).Value, // каждый день в 6:00, сразу после подъёта
      TimeSpan.FromHours(2)
    ),
    new TaskTemplateData(
      "Проверить и долить воду хомяку",
      2,
      Schedule.CreateDaily(new TimeOnly(6, 0)).Value, // каждый день в 6:00, сразу после подъёта
      TimeSpan.FromHours(2)
    ),

    // Периодические задачи
    new TaskTemplateData(
      "Убрать клетку хомяка",
      3,
      Schedule.CreateWeekly(new TimeOnly(12, 0), DayOfWeek.Saturday).Value, // раз в неделю, в субботу в 12:00
      TimeSpan.FromHours(10)
    ),
    new TaskTemplateData(
      "Полностью помыть клетку хомяка",
      3,
      Schedule.CreateMonthly(new TimeOnly(12, 0), 1).Value, // раз в месяц, 1-го числа в 12:00
      TimeSpan.FromHours(10)
    ),
    new TaskTemplateData(
      "Проверить игрушки и колесо хомяка",
      2,
      Schedule.CreateWeekly(new TimeOnly(19, 0), DayOfWeek.Sunday).Value, // раз в неделю, в воскресенье в 19:00
      TimeSpan.FromHours(3)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> ParrotTemplates = new[]
  {
    // Ежедневные задачи
    new TaskTemplateData(
      "Покормить попугая",
      2,
      Schedule.CreateDaily(new TimeOnly(8, 0)).Value, // каждый день в 8:00, перед уходом из дома
      TimeSpan.FromHours(2)
    ),
    new TaskTemplateData(
      "Поменять воду попугаю",
      2,
      Schedule.CreateDaily(new TimeOnly(8, 0)).Value, // каждый день в 8:00, вместе с кормлением
      TimeSpan.FromHours(2)
    ),
    new TaskTemplateData(
      "Поиграть и пообщаться с попугаем",
      2,
      Schedule.CreateDaily(new TimeOnly(18, 0)).Value, // каждый день в 18:00, после работы
      TimeSpan.FromHours(3)
    ),

    // Периодические задачи
    new TaskTemplateData(
      "Убрать поддон клетки попугая",
      3,
      Schedule.CreateWeekly(new TimeOnly(10, 0), DayOfWeek.Saturday).Value, // раз в неделю, в субботу утром
      TimeSpan.FromHours(6)
    ),
    new TaskTemplateData(
      "Проверить игрушки и жердочки попугая",
      2,
      Schedule.CreateWeekly(new TimeOnly(19, 0), DayOfWeek.Wednesday).Value, // раз в неделю, в среду вечером
      TimeSpan.FromHours(4)
    ),
    new TaskTemplateData(
      "Полностью помыть клетку попугая",
      3,
      Schedule.CreateMonthly(new TimeOnly(12, 0), 1).Value, // раз в месяц, 1-го числа в 12:00
      TimeSpan.FromHours(12)
    )
  };

  public static IReadOnlyList<TaskTemplateData> GetDefaultTemplates(PetType petType) =>
    petType switch
    {
      PetType.Cat => CatTemplates,
      PetType.Dog => DogTemplates,
      PetType.Hamster => HamsterTemplates,
      PetType.Parrot => ParrotTemplates,
      _ => throw new ArgumentOutOfRangeException(nameof(petType), petType, "Unknown pet type")
    };

  public record TaskTemplateData(string Title, int Points, Schedule Schedule, TimeSpan DueDuration)
  {
    public TaskPoints GetTaskPoints() => new(Points);
  }
}
