namespace FamilyTaskManager.UseCases.Features.SpotManagement.Dtos;

/// <summary>
///   Defines default task templates for each Spot type.
///   Based on the Spot care requirements documented in docs/MVP1/Шаблоны спотов и задач.md
/// </summary>
public static class SpotTaskTemplateData
{
  private static readonly IReadOnlyList<TaskTemplateData> CatTemplates =
  [
    // Ежедневные задачи
    new(
      "Убрать какахи из лотка кота",
      TaskPoints.Hard,
      Schedule.CreateDaily(new(5, 30)).Value, // каждый день в 5:30, до подъёма семьи
      TimeSpan.FromHours(2)
    ),
    new(
      "Убрать комки из лотка кота",
      TaskPoints.Medium,
      Schedule.CreateDaily(new(20, 0)).Value, // каждый день в 20:00, после возвращения с работы
      TimeSpan.FromHours(2)
    ),
    new(
      "Налить свежую воду коту",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(6, 0)).Value, // каждый день в 6:00, сразу после подъёта
      TimeSpan.FromHours(2)
    ),
    new(
      "Насыпать корм коту",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(6, 0)).Value, // каждый день в 6:00, сразу после подъёта
      TimeSpan.FromHours(1)
    ),

    // Периодические задачи
    new(
      "Полностью заменить наполнитель в лотке кота",
      TaskPoints.VeryHard,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Sunday).Value, // раз в неделю в 12:00
      TimeSpan.FromHours(10)
    ),
    new(
      "Помыть миски кота",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Monday)
        .Value, // 2 раза в неделю (используем понедельник и четверг)
      TimeSpan.FromHours(3)
    ),
    new(
      "Почистить место для сна кота",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Sunday).Value, // раз в неделю, в воскресенье в 12:00
      TimeSpan.FromHours(10)
    ),
    new(
      "Поиграть с котом",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(19, 0)).Value, // каждый день в 19:00, после возвращения с работы
      TimeSpan.FromHours(3)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> DogTemplates =
  [
    // Ежедневные задачи
    new(
      "Выгулять собаку утром",
      TaskPoints.Hard,
      Schedule.CreateDaily(new(6, 0)).Value, // каждый день в 6:00, сразу после подъёта
      TimeSpan.FromHours(1.5)
    ),
    new(
      "Выгулять собаку вечером",
      TaskPoints.Hard,
      Schedule.CreateDaily(new(19, 0)).Value, // каждый день в 19:00, сразу после возвращения с работы
      TimeSpan.FromHours(3)
    ),
    new(
      "Накормить собаку",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(6, 0)).Value, // каждый день в 6:00, сразу после подъёта
      TimeSpan.FromHours(2)
    ),

    // Периодические задачи
    new(
      "Помыть миски собаки",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Monday).Value, // 2 раза в неделю
      TimeSpan.FromHours(3)
    ),
    new(
      "Расчесать собаку",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Saturday).Value, // раз в неделю, в субботу в 19:00
      TimeSpan.FromHours(3)
    ),
    new(
      "Искупать собаку",
      TaskPoints.VeryHard,
      Schedule.CreateMonthly(new(12, 0), 1).Value, // раз в месяц, 1-го числа в 12:00
      TimeSpan.FromHours(10)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> HamsterTemplates =
  [
    // Ежедневные задачи
    new(
      "Насыпать корм хомяку",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(6, 0)).Value, // каждый день в 6:00, сразу после подъёта
      TimeSpan.FromHours(2)
    ),
    new(
      "Проверить и долить воду хомяку",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(6, 0)).Value, // каждый день в 6:00, сразу после подъёта
      TimeSpan.FromHours(2)
    ),

    // Периодические задачи
    new(
      "Убрать клетку хомяка",
      TaskPoints.Hard,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Saturday).Value, // раз в неделю, в субботу в 12:00
      TimeSpan.FromHours(10)
    ),
    new(
      "Полностью помыть клетку хомяка",
      TaskPoints.VeryHard,
      Schedule.CreateMonthly(new(12, 0), 1).Value, // раз в месяц, 1-го числа в 12:00
      TimeSpan.FromHours(10)
    ),
    new(
      "Проверить игрушки и колесо хомяка",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Sunday).Value, // раз в неделю, в воскресенье в 19:00
      TimeSpan.FromHours(3)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> ParrotTemplates =
  [
    // Ежедневные задачи
    new(
      "Покормить попугая",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(8, 0)).Value, // каждый день в 8:00, перед уходом из дома
      TimeSpan.FromHours(2)
    ),
    new(
      "Поменять воду попугаю",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(8, 0)).Value, // каждый день в 8:00, вместе с кормлением
      TimeSpan.FromHours(2)
    ),
    new(
      "Поиграть и пообщаться с попугаем",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(18, 0)).Value, // каждый день в 18:00, после работы
      TimeSpan.FromHours(3)
    ),

    // Периодические задачи
    new(
      "Убрать поддон клетки попугая",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(10, 0), DayOfWeek.Saturday).Value, // раз в неделю, в субботу утром
      TimeSpan.FromHours(6)
    ),
    new(
      "Проверить игрушки и жердочки попугая",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Wednesday).Value, // раз в неделю, в среду вечером
      TimeSpan.FromHours(4)
    ),
    new(
      "Полностью помыть клетку попугая",
      TaskPoints.VeryHard,
      Schedule.CreateMonthly(new(12, 0), 1).Value, // раз в месяц, 1-го числа в 12:00
      TimeSpan.FromHours(12)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> OtherPetTemplates =
  [
    // Ежедневные задачи (универсальные для другого питомца)
    new(
      "Накормить питомца",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(8, 0)).Value, // каждый день в 8:00
      TimeSpan.FromHours(2)
    ),
    new(
      "Поменять воду питомцу",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(8, 30)).Value, // каждый день в 8:30
      TimeSpan.FromHours(2)
    ),

    // Периодические задачи
    new(
      "Убрать место проживания питомца",
      TaskPoints.Hard,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Saturday).Value, // раз в неделю, в субботу в 12:00
      TimeSpan.FromHours(10)
    ),
    new(
      "Поиграть и пообщаться с питомцем",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(19, 0)).Value, // каждый день в 19:00
      TimeSpan.FromHours(3)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> FishTemplates =
  [
    // Ежедневные задачи
    new(
      "Покормить рыбок в аквариуме",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(8, 0)).Value,
      TimeSpan.FromHours(2)
    ),

    // Периодические задачи
    new(
      "Сделать подмену воды в аквариуме",
      TaskPoints.Hard,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(10)
    ),
    new(
      "Почистить фильтр и стёкла аквариума",
      TaskPoints.VeryHard,
      Schedule.CreateMonthly(new(12, 0), 1).Value,
      TimeSpan.FromHours(12)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> TurtleTemplates =
  [
    new(
      "Покормить черепаху",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(9, 0)).Value,
      TimeSpan.FromHours(2)
    ),
    new(
      "Проверить и сменить воду в террариуме",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(9, 30)).Value,
      TimeSpan.FromHours(2)
    ),
    new(
      "Убрать и помыть террариум",
      TaskPoints.VeryHard,
      Schedule.CreateWeekly(new(11, 0), DayOfWeek.Sunday).Value,
      TimeSpan.FromHours(10)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> PlantTemplates =
  [
    new(
      "Полить комнатные растения",
      TaskPoints.Easy,
      Schedule.CreateWeekly(new(10, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(8)
    ),
    new(
      "Опрыскать листья растений",
      TaskPoints.Easy,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Wednesday).Value,
      TimeSpan.FromHours(4)
    ),
    new(
      "Подкормить и удобрить растения",
      TaskPoints.Medium,
      Schedule.CreateMonthly(new(12, 0), 1).Value,
      TimeSpan.FromHours(12)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> KitchenTemplates =
  [
    new(
      "Помыть посуду и раковину на кухне",
      TaskPoints.Medium,
      Schedule.CreateDaily(new(20, 0)).Value,
      TimeSpan.FromHours(3)
    ),
    new(
      "Протереть стол и рабочие поверхности на кухне",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(20, 30)).Value,
      TimeSpan.FromHours(3)
    ),
    new(
      "Выбросить мусор с кухни",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(21, 0)).Value,
      TimeSpan.FromHours(3)
    ),
    new(
      "Сделать небольшую генеральную уборку на кухне",
      TaskPoints.VeryHard,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(10)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> BathroomTemplates =
  [
    new(
      "Протереть раковину и зеркало в ванной",
      TaskPoints.Easy,
      Schedule.CreateWeekly(new(20, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(4)
    ),
    new(
      "Помыть унитаз",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(11, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(8)
    ),
    new(
      "Вымыть пол в ванной",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(11, 30), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(8)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> KidsRoomTemplates =
  [
    new(
      "Собрать и разложить игрушки в детской",
      TaskPoints.Medium,
      Schedule.CreateDaily(new(19, 30)).Value,
      TimeSpan.FromHours(3)
    ),
    new(
      "Заправить кровать в детской",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(8, 0)).Value,
      TimeSpan.FromHours(3)
    ),
    new(
      "Навести порядок на рабочем столе ребёнка",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(18, 0), DayOfWeek.Sunday).Value,
      TimeSpan.FromHours(6)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> HallwayTemplates =
  [
    new(
      "Разложить обувь и верхнюю одежду в прихожей",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(20, 0)).Value,
      TimeSpan.FromHours(3)
    ),
    new(
      "Подмести или пропылесосить в прихожей",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(20, 30), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(4)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> WashingMachineTemplates =
  [
    new(
      "Запустить стирку вещей",
      TaskPoints.Easy,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(6)
    ),
    new(
      "Почистить фильтр стиральной машины",
      TaskPoints.Hard,
      Schedule.CreateMonthly(new(18, 0), 1).Value,
      TimeSpan.FromHours(12)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> DishwasherTemplates =
  [
    new(
      "Запустить посудомойку с грязной посудой",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(21, 0)).Value,
      TimeSpan.FromHours(6)
    ),
    new(
      "Почистить фильтр посудомойки",
      TaskPoints.Hard,
      Schedule.CreateWeekly(new(18, 0), DayOfWeek.Sunday).Value,
      TimeSpan.FromHours(12)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> FridgeTemplates =
  [
    new(
      "Проверить продукты в холодильнике и выбросить испорченное",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(18, 0), DayOfWeek.Friday).Value,
      TimeSpan.FromHours(6)
    ),
    new(
      "Протереть полки и стенки в холодильнике",
      TaskPoints.Hard,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(12)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> FinancesTemplates =
  [
    new(
      "Проверить семейный бюджет и траты",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(20, 0), DayOfWeek.Sunday).Value,
      TimeSpan.FromHours(12)
    ),
    new(
      "Оплатить регулярные счета семьи",
      TaskPoints.Medium,
      Schedule.CreateMonthly(new(19, 0), 1).Value,
      TimeSpan.FromHours(48)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> OtherTemplates = [];

  private static readonly IReadOnlyList<TaskTemplateData> DocumentsTemplates =
  [
    new(
      "Разобрать бумажные документы и убрать лишнее",
      TaskPoints.Medium,
      Schedule.CreateMonthly(new(18, 0), 1).Value,
      TimeSpan.FromHours(24)
    ),
    new(
      "Проверить сроки действия важных документов семьи",
      TaskPoints.Medium,
      Schedule.CreateMonthly(new(19, 0), 1).Value,
      TimeSpan.FromHours(48)
    )
  ];

  public static IReadOnlyList<TaskTemplateData> GetDefaultTemplates(SpotType spotType) =>
    spotType switch
    {
      SpotType.Cat => CatTemplates,
      SpotType.Dog => DogTemplates,
      SpotType.Hamster => HamsterTemplates,
      SpotType.Parrot => ParrotTemplates,
      SpotType.OtherPet => OtherPetTemplates,
      SpotType.Fish => FishTemplates,
      SpotType.Turtle => TurtleTemplates,
      SpotType.Plant => PlantTemplates,
      SpotType.Kitchen => KitchenTemplates,
      SpotType.Bathroom => BathroomTemplates,
      SpotType.KidsRoom => KidsRoomTemplates,
      SpotType.Hallway => HallwayTemplates,
      SpotType.WashingMachine => WashingMachineTemplates,
      SpotType.Dishwasher => DishwasherTemplates,
      SpotType.Fridge => FridgeTemplates,
      SpotType.Finances => FinancesTemplates,
      SpotType.Documents => DocumentsTemplates,
      SpotType.Other => OtherTemplates,
      _ => throw new ArgumentOutOfRangeException(nameof(spotType), spotType, "Unknown Spot type")
    };

  public record TaskTemplateData(string Title, TaskPoints Points, Schedule Schedule, TimeSpan DueDuration)
  {
    public TaskPoints GetTaskPoints() => Points;
  }
}
