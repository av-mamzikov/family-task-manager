namespace FamilyTaskManager.UseCases.Spots;

/// <summary>
///   Defines default task templates for each Spot type.
///   Based on the Spot care requirements documented in docs/MVP1/Шаблоны спотов и задач.md
/// </summary>
public static class SpotTaskTemplateData
{
  private static readonly IReadOnlyList<TaskTemplateData> CatTemplates = new[]
  {
    // Ежедневные задачи
    new TaskTemplateData(
      "Убрать какахи из лотка кота",
      TaskPoints.Hard,
      Schedule.CreateDaily(new(5, 30)).Value, // каждый день в 5:30, до подъёма семьи
      TimeSpan.FromHours(2)
    ),
    new TaskTemplateData(
      "Убрать комки из лотка кота",
      TaskPoints.Medium,
      Schedule.CreateDaily(new(20, 0)).Value, // каждый день в 20:00, после возвращения с работы
      TimeSpan.FromHours(2)
    ),
    new TaskTemplateData(
      "Налить свежую воду коту",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(6, 0)).Value, // каждый день в 6:00, сразу после подъёта
      TimeSpan.FromHours(2)
    ),
    new TaskTemplateData(
      "Насыпать корм коту",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(6, 0)).Value, // каждый день в 6:00, сразу после подъёта
      TimeSpan.FromHours(1)
    ),

    // Периодические задачи
    new TaskTemplateData(
      "Полностью заменить наполнитель в лотке кота",
      TaskPoints.VeryHard,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Sunday).Value, // раз в неделю в 12:00
      TimeSpan.FromHours(10)
    ),
    new TaskTemplateData(
      "Помыть миски кота",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Monday)
        .Value, // 2 раза в неделю (используем понедельник и четверг)
      TimeSpan.FromHours(3)
    ),
    new TaskTemplateData(
      "Почистить место для сна кота",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Sunday).Value, // раз в неделю, в воскресенье в 12:00
      TimeSpan.FromHours(10)
    ),
    new TaskTemplateData(
      "Поиграть с котом",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(19, 0)).Value, // каждый день в 19:00, после возвращения с работы
      TimeSpan.FromHours(3)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> DogTemplates = new[]
  {
    // Ежедневные задачи
    new TaskTemplateData(
      "Выгулять собаку утром",
      TaskPoints.Hard,
      Schedule.CreateDaily(new(6, 0)).Value, // каждый день в 6:00, сразу после подъёта
      TimeSpan.FromHours(1.5)
    ),
    new TaskTemplateData(
      "Выгулять собаку вечером",
      TaskPoints.Hard,
      Schedule.CreateDaily(new(19, 0)).Value, // каждый день в 19:00, сразу после возвращения с работы
      TimeSpan.FromHours(3)
    ),
    new TaskTemplateData(
      "Накормить собаку",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(6, 0)).Value, // каждый день в 6:00, сразу после подъёта
      TimeSpan.FromHours(2)
    ),

    // Периодические задачи
    new TaskTemplateData(
      "Помыть миски собаки",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Monday).Value, // 2 раза в неделю
      TimeSpan.FromHours(3)
    ),
    new TaskTemplateData(
      "Расчесать собаку",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Saturday).Value, // раз в неделю, в субботу в 19:00
      TimeSpan.FromHours(3)
    ),
    new TaskTemplateData(
      "Искупать собаку",
      TaskPoints.VeryHard,
      Schedule.CreateMonthly(new(12, 0), 1).Value, // раз в месяц, 1-го числа в 12:00
      TimeSpan.FromHours(10)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> HamsterTemplates = new[]
  {
    // Ежедневные задачи
    new TaskTemplateData(
      "Насыпать корм хомяку",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(6, 0)).Value, // каждый день в 6:00, сразу после подъёта
      TimeSpan.FromHours(2)
    ),
    new TaskTemplateData(
      "Проверить и долить воду хомяку",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(6, 0)).Value, // каждый день в 6:00, сразу после подъёта
      TimeSpan.FromHours(2)
    ),

    // Периодические задачи
    new TaskTemplateData(
      "Убрать клетку хомяка",
      TaskPoints.Hard,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Saturday).Value, // раз в неделю, в субботу в 12:00
      TimeSpan.FromHours(10)
    ),
    new TaskTemplateData(
      "Полностью помыть клетку хомяка",
      TaskPoints.VeryHard,
      Schedule.CreateMonthly(new(12, 0), 1).Value, // раз в месяц, 1-го числа в 12:00
      TimeSpan.FromHours(10)
    ),
    new TaskTemplateData(
      "Проверить игрушки и колесо хомяка",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Sunday).Value, // раз в неделю, в воскресенье в 19:00
      TimeSpan.FromHours(3)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> ParrotTemplates = new[]
  {
    // Ежедневные задачи
    new TaskTemplateData(
      "Покормить попугая",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(8, 0)).Value, // каждый день в 8:00, перед уходом из дома
      TimeSpan.FromHours(2)
    ),
    new TaskTemplateData(
      "Поменять воду попугаю",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(8, 0)).Value, // каждый день в 8:00, вместе с кормлением
      TimeSpan.FromHours(2)
    ),
    new TaskTemplateData(
      "Поиграть и пообщаться с попугаем",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(18, 0)).Value, // каждый день в 18:00, после работы
      TimeSpan.FromHours(3)
    ),

    // Периодические задачи
    new TaskTemplateData(
      "Убрать поддон клетки попугая",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(10, 0), DayOfWeek.Saturday).Value, // раз в неделю, в субботу утром
      TimeSpan.FromHours(6)
    ),
    new TaskTemplateData(
      "Проверить игрушки и жердочки попугая",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Wednesday).Value, // раз в неделю, в среду вечером
      TimeSpan.FromHours(4)
    ),
    new TaskTemplateData(
      "Полностью помыть клетку попугая",
      TaskPoints.VeryHard,
      Schedule.CreateMonthly(new(12, 0), 1).Value, // раз в месяц, 1-го числа в 12:00
      TimeSpan.FromHours(12)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> OtherPetTemplates = new[]
  {
    // Ежедневные задачи (универсальные для другого питомца)
    new TaskTemplateData(
      "Накормить питомца",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(8, 0)).Value, // каждый день в 8:00
      TimeSpan.FromHours(2)
    ),
    new TaskTemplateData(
      "Поменять воду питомцу",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(8, 30)).Value, // каждый день в 8:30
      TimeSpan.FromHours(2)
    ),

    // Периодические задачи
    new TaskTemplateData(
      "Убрать место проживания питомца",
      TaskPoints.Hard,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Saturday).Value, // раз в неделю, в субботу в 12:00
      TimeSpan.FromHours(10)
    ),
    new TaskTemplateData(
      "Поиграть и пообщаться с питомцем",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(19, 0)).Value, // каждый день в 19:00
      TimeSpan.FromHours(3)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> FishTemplates = new[]
  {
    // Ежедневные задачи
    new TaskTemplateData(
      "Покормить рыбок в аквариуме",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(8, 0)).Value,
      TimeSpan.FromHours(2)
    ),

    // Периодические задачи
    new TaskTemplateData(
      "Сделать подмену воды в аквариуме",
      TaskPoints.Hard,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(10)
    ),
    new TaskTemplateData(
      "Почистить фильтр и стёкла аквариума",
      TaskPoints.VeryHard,
      Schedule.CreateMonthly(new(12, 0), 1).Value,
      TimeSpan.FromHours(12)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> TurtleTemplates = new[]
  {
    new TaskTemplateData(
      "Покормить черепаху",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(9, 0)).Value,
      TimeSpan.FromHours(2)
    ),
    new TaskTemplateData(
      "Проверить и сменить воду в террариуме",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(9, 30)).Value,
      TimeSpan.FromHours(2)
    ),
    new TaskTemplateData(
      "Убрать и помыть террариум",
      TaskPoints.VeryHard,
      Schedule.CreateWeekly(new(11, 0), DayOfWeek.Sunday).Value,
      TimeSpan.FromHours(10)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> PlantTemplates = new[]
  {
    new TaskTemplateData(
      "Полить комнатные растения",
      TaskPoints.Easy,
      Schedule.CreateWeekly(new(10, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(8)
    ),
    new TaskTemplateData(
      "Опрыскать листья растений",
      TaskPoints.Easy,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Wednesday).Value,
      TimeSpan.FromHours(4)
    ),
    new TaskTemplateData(
      "Подкормить и удобрить растения",
      TaskPoints.Medium,
      Schedule.CreateMonthly(new(12, 0), 1).Value,
      TimeSpan.FromHours(12)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> KitchenTemplates = new[]
  {
    new TaskTemplateData(
      "Помыть посуду и раковину на кухне",
      TaskPoints.Medium,
      Schedule.CreateDaily(new(20, 0)).Value,
      TimeSpan.FromHours(3)
    ),
    new TaskTemplateData(
      "Протереть стол и рабочие поверхности на кухне",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(20, 30)).Value,
      TimeSpan.FromHours(3)
    ),
    new TaskTemplateData(
      "Выбросить мусор с кухни",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(21, 0)).Value,
      TimeSpan.FromHours(3)
    ),
    new TaskTemplateData(
      "Сделать небольшую генеральную уборку на кухне",
      TaskPoints.VeryHard,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(10)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> BathroomTemplates = new[]
  {
    new TaskTemplateData(
      "Протереть раковину и зеркало в ванной",
      TaskPoints.Easy,
      Schedule.CreateWeekly(new(20, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(4)
    ),
    new TaskTemplateData(
      "Помыть унитаз",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(11, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(8)
    ),
    new TaskTemplateData(
      "Вымыть пол в ванной",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(11, 30), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(8)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> KidsRoomTemplates = new[]
  {
    new TaskTemplateData(
      "Собрать и разложить игрушки в детской",
      TaskPoints.Medium,
      Schedule.CreateDaily(new(19, 30)).Value,
      TimeSpan.FromHours(3)
    ),
    new TaskTemplateData(
      "Заправить кровать в детской",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(8, 0)).Value,
      TimeSpan.FromHours(3)
    ),
    new TaskTemplateData(
      "Навести порядок на рабочем столе ребёнка",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(18, 0), DayOfWeek.Sunday).Value,
      TimeSpan.FromHours(6)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> HallwayTemplates = new[]
  {
    new TaskTemplateData(
      "Разложить обувь и верхнюю одежду в прихожей",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(20, 0)).Value,
      TimeSpan.FromHours(3)
    ),
    new TaskTemplateData(
      "Подмести или пропылесосить в прихожей",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(20, 30), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(4)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> WashingMachineTemplates = new[]
  {
    new TaskTemplateData(
      "Запустить стирку вещей",
      TaskPoints.Easy,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(6)
    ),
    new TaskTemplateData(
      "Почистить фильтр стиральной машины",
      TaskPoints.Hard,
      Schedule.CreateMonthly(new(18, 0), 1).Value,
      TimeSpan.FromHours(12)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> DishwasherTemplates = new[]
  {
    new TaskTemplateData(
      "Запустить посудомойку с грязной посудой",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(21, 0)).Value,
      TimeSpan.FromHours(6)
    ),
    new TaskTemplateData(
      "Почистить фильтр посудомойки",
      TaskPoints.Hard,
      Schedule.CreateWeekly(new(18, 0), DayOfWeek.Sunday).Value,
      TimeSpan.FromHours(12)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> FridgeTemplates = new[]
  {
    new TaskTemplateData(
      "Проверить продукты в холодильнике и выбросить испорченное",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(18, 0), DayOfWeek.Friday).Value,
      TimeSpan.FromHours(6)
    ),
    new TaskTemplateData(
      "Протереть полки и стенки в холодильнике",
      TaskPoints.Hard,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(12)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> FinancesTemplates = new[]
  {
    new TaskTemplateData(
      "Проверить семейный бюджет и траты",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(20, 0), DayOfWeek.Sunday).Value,
      TimeSpan.FromHours(12)
    ),
    new TaskTemplateData(
      "Оплатить регулярные счета семьи",
      TaskPoints.Medium,
      Schedule.CreateMonthly(new(19, 0), 1).Value,
      TimeSpan.FromHours(48)
    )
  };

  private static readonly IReadOnlyList<TaskTemplateData> DocumentsTemplates = new[]
  {
    new TaskTemplateData(
      "Разобрать бумажные документы и убрать лишнее",
      TaskPoints.Medium,
      Schedule.CreateMonthly(new(18, 0), 1).Value,
      TimeSpan.FromHours(24)
    ),
    new TaskTemplateData(
      "Проверить сроки действия важных документов семьи",
      TaskPoints.Medium,
      Schedule.CreateMonthly(new(19, 0), 1).Value,
      TimeSpan.FromHours(48)
    )
  };

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
      _ => throw new ArgumentOutOfRangeException(nameof(spotType), spotType, "Unknown Spot type")
    };

  public record TaskTemplateData(string Title, TaskPoints Points, Schedule Schedule, TimeSpan DueDuration)
  {
    public TaskPoints GetTaskPoints() => Points;
  }
}
