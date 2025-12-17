namespace FamilyTaskManager.UseCases.Features.SpotManagement.Dtos;

/// <summary>
///   Defines default task templates for each Spot type.
///   Based on the Spot care requirements documented in docs/MVP1/Шаблоны спотов и задач.md
/// </summary>
public static class SpotTaskTemplateData
{
  private static readonly IReadOnlyList<TaskTemplateData> _catTemplates =
  [
    new(
      "Убрать какахи из лотка кота",
      TaskPoints.Hard,
      Schedule.CreateDaily(new(5, 30)).Value,
      TimeSpan.FromHours(2)
    ),
    new(
      "Убрать комки из лотка кота",
      TaskPoints.Medium,
      Schedule.CreateDaily(new(20, 0)).Value,
      TimeSpan.FromHours(2)
    ),
    new(
      "Налить свежую воду коту",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(6, 0)).Value,
      TimeSpan.FromHours(2)
    ),
    new(
      "Насыпать корм коту",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(6, 0)).Value,
      TimeSpan.FromHours(1)
    ),

    new(
      "Полностью заменить наполнитель в лотке кота",
      TaskPoints.VeryHard,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Sunday).Value,
      TimeSpan.FromHours(10)
    ),
    new(
      "Помыть миски кота",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Monday)
        .Value,
      TimeSpan.FromHours(3)
    ),
    new(
      "Почистить место для сна кота",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Sunday).Value,
      TimeSpan.FromHours(10)
    ),
    new(
      "Поиграть с котом",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(19, 0)).Value,
      TimeSpan.FromHours(3)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> _dogTemplates =
  [
    new(
      "Выгулять собаку утром",
      TaskPoints.Hard,
      Schedule.CreateDaily(new(6, 0)).Value,
      TimeSpan.FromHours(1.5)
    ),
    new(
      "Выгулять собаку вечером",
      TaskPoints.Hard,
      Schedule.CreateDaily(new(19, 0)).Value,
      TimeSpan.FromHours(3)
    ),
    new(
      "Накормить собаку",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(6, 0)).Value,
      TimeSpan.FromHours(2)
    ),

    new(
      "Помыть миски собаки",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Monday).Value,
      TimeSpan.FromHours(3)
    ),
    new(
      "Расчесать собаку",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(3)
    ),
    new(
      "Искупать собаку",
      TaskPoints.VeryHard,
      Schedule.CreateMonthly(new(12, 0), 1).Value,
      TimeSpan.FromHours(10)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> _hamsterTemplates =
  [
    new(
      "Насыпать корм хомяку",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(6, 0)).Value,
      TimeSpan.FromHours(2)
    ),
    new(
      "Проверить и долить воду хомяку",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(6, 0)).Value,
      TimeSpan.FromHours(2)
    ),

    new(
      "Убрать клетку хомяка",
      TaskPoints.Hard,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(10)
    ),
    new(
      "Полностью помыть клетку хомяка",
      TaskPoints.VeryHard,
      Schedule.CreateMonthly(new(12, 0), 1).Value,
      TimeSpan.FromHours(10)
    ),
    new(
      "Проверить игрушки и колесо хомяка",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Sunday).Value,
      TimeSpan.FromHours(3)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> _parrotTemplates =
  [
    new(
      "Покормить попугая",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(8, 0)).Value,
      TimeSpan.FromHours(2)
    ),
    new(
      "Поменять воду попугаю",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(8, 0)).Value,
      TimeSpan.FromHours(2)
    ),
    new(
      "Поиграть и пообщаться с попугаем",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(18, 0)).Value,
      TimeSpan.FromHours(3)
    ),

    new(
      "Убрать поддон клетки попугая",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(10, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(6)
    ),
    new(
      "Проверить игрушки и жердочки попугая",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(19, 0), DayOfWeek.Wednesday).Value,
      TimeSpan.FromHours(4)
    ),
    new(
      "Полностью помыть клетку попугая",
      TaskPoints.VeryHard,
      Schedule.CreateMonthly(new(12, 0), 1).Value,
      TimeSpan.FromHours(12)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> _otherPetTemplates =
  [
    new(
      "Накормить питомца",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(8, 0)).Value,
      TimeSpan.FromHours(2)
    ),
    new(
      "Поменять воду питомцу",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(8, 30)).Value,
      TimeSpan.FromHours(2)
    ),

    new(
      "Убрать место проживания питомца",
      TaskPoints.Hard,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(10)
    ),
    new(
      "Поиграть и пообщаться с питомцем",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(19, 0)).Value,
      TimeSpan.FromHours(3)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> _fishTemplates =
  [
    new(
      "Покормить рыбок в аквариуме",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(8, 0)).Value,
      TimeSpan.FromHours(2)
    ),

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

  private static readonly IReadOnlyList<TaskTemplateData> _turtleTemplates =
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

  private static readonly IReadOnlyList<TaskTemplateData> _plantTemplates =
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

  private static readonly IReadOnlyList<TaskTemplateData> _kitchenTemplates =
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

  private static readonly IReadOnlyList<TaskTemplateData> _bathroomTemplates =
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

  private static readonly IReadOnlyList<TaskTemplateData> _kidsRoomTemplates =
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

  private static readonly IReadOnlyList<TaskTemplateData> _hallwayTemplates =
  [
    new(
      "Разложить обувь и верхнюю одежду в прихожей",
      TaskPoints.Easy,
      Schedule.CreateWeekly(new(20, 0), DayOfWeek.Sunday).Value,
      TimeSpan.FromHours(3)
    ),
    new(
      "Подмести или пропылесосить в прихожей",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(20, 30), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(4)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> _bedroomTemplates =
  [
    new(
      "Заправить кровать в спальне",
      TaskPoints.Easy,
      Schedule.CreateDaily(new(8, 0)).Value,
      TimeSpan.FromHours(3)
    ),
    new(
      "Протереть пыль в спальне",
      TaskPoints.Easy,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(10)
    ),
    new(
      "Сменить постельное бельё в спальне",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(10)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> _livingRoomTemplates =
  [
    new(
      "Протереть пыль в гостиной",
      TaskPoints.Easy,
      Schedule.CreateWeekly(new(12, 0), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(10)
    ),
    new(
      "Пропылесосить/помыть пол в гостиной",
      TaskPoints.Medium,
      Schedule.CreateWeekly(new(12, 30), DayOfWeek.Saturday).Value,
      TimeSpan.FromHours(10)
    )
  ];

  private static readonly IReadOnlyList<TaskTemplateData> _washingMachineTemplates =
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

  private static readonly IReadOnlyList<TaskTemplateData> _dishwasherTemplates =
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

  private static readonly IReadOnlyList<TaskTemplateData> _fridgeTemplates =
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

  private static readonly IReadOnlyList<TaskTemplateData> _financesTemplates =
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

  private static readonly IReadOnlyList<TaskTemplateData> _otherTemplates = [];

  private static readonly IReadOnlyList<TaskTemplateData> _documentsTemplates =
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
      SpotType.Cat => _catTemplates,
      SpotType.Dog => _dogTemplates,
      SpotType.Hamster => _hamsterTemplates,
      SpotType.Parrot => _parrotTemplates,
      SpotType.OtherPet => _otherPetTemplates,
      SpotType.Fish => _fishTemplates,
      SpotType.Turtle => _turtleTemplates,
      SpotType.Plant => _plantTemplates,
      SpotType.Kitchen => _kitchenTemplates,
      SpotType.Bathroom => _bathroomTemplates,
      SpotType.KidsRoom => _kidsRoomTemplates,
      SpotType.Hallway => _hallwayTemplates,
      SpotType.Bedroom => _bedroomTemplates,
      SpotType.LivingRoom => _livingRoomTemplates,
      SpotType.WashingMachine => _washingMachineTemplates,
      SpotType.Dishwasher => _dishwasherTemplates,
      SpotType.Fridge => _fridgeTemplates,
      SpotType.Finances => _financesTemplates,
      SpotType.Documents => _documentsTemplates,
      SpotType.Other => _otherTemplates,
      _ => throw new ArgumentOutOfRangeException(nameof(spotType), spotType, "Unknown Spot type")
    };

  public record TaskTemplateData(string Title, TaskPoints Points, Schedule Schedule, TimeSpan DueDuration)
  {
    public TaskPoints GetTaskPoints() => Points;
  }
}
