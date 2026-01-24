using FamilyTaskManager.Core.TaskAggregate;

namespace FamilyTaskManager.Host.Modules.Bot.Constants;

public static class BotMessages
{
  public static class Success
  {
    public const string NextStepsMessage = "🎉 Отлично! Теперь ваша семья готова к использованию.\n\n" +
                                           "📋 *Что дальше?*\n\n" +
                                           "🧩 Добавьте спотов, которые будут выполнять задачи\n" +
                                           "📝 Просматривайте и берите в работу актуальные задачи\n" +
                                           "📋 Управляйте шаблонами задач (автоматическое создание по расписанию)\n" +
                                           "🏆 Следите за статистикой и достижениями семьи\n" +
                                           "👨‍👩‍👧‍👦 Управляйте членами семьи и настройками\n\n" +
                                           "💡 *Совет:* Начните с добавления спота, а затем создавайте шаблоны задач для него!";

    public const string FamilySelected = "✅ Семья выбрана!\n\n";

    public static string FamilyCreatedMessage(string familyName) => $"✅ Семья \"{familyName}\" успешно создана!\n\n";
  }

  public static class Errors
  {
    public const string NoFamily = "❌ Сначала выберите активную семью через 🏠 Семья";
    public const string UnknownError = "❌ Ошибка. Попробуйте /start";
    public const string LocationError = "❌ Ошибка определения временной зоны по геолокации.\n\n";
    public const string TryAgain = "Пожалуйста, попробуйте снова.";
    public const string TryAgainOrChooseTimezone = "Пожалуйста, попробуйте снова или выберите временную зону вручную.";
    public const string ChooseTimezoneManually = "Пожалуйста, выберите временную зону вручную.";
    public const string InvalidCron = "Проверьте правильность cron-выражения.";
    public const string TasksLoadError = "❌ Ошибка загрузки задач";
    public const string UnknownCommand = "❓ Неизвестная команда. Используйте /help для списка команд.";
    public const string NoSpots = "❌ В семье нет спотов. Сначала создайте спот через \"🧩 Споты\"";
    public const string FamilyNameTooShort = "❌ Название семьи должно содержать минимум 3 символа. Попробуйте снова:";
    public const string SessionError = "❌ Ошибка. Попробуйте создать семью заново.";
    public const string SessionErrorRetry = "❌ Ошибка сессии. Попробуйте создать семью заново.";
    public const string InvalidLocationData = "❌ Получены некорректные данные о местоположении.\n\n";

    public const string TimezoneDetectionFailed = "❌ Не удалось определить временную зону для вашей локации.\n\n" +
                                                  "Пожалуйста, выберите временную зону вручную.";

    public const string TimezoneValidationFailed = "❌ Не удалось определить временную зону для вашей локации.\n\n";

    public static string FamilyCreationError(string? error) => $"❌ Ошибка создания семьи: {error}";
  }

  public static class Messages
  {
    public const string WelcomeMessage = "👋 Добро пожаловать в Семейный менеджер дел!\n\n"
                                         + "Помогите вашей семье организовать быт и достигать целей вместе!\n\n";

    public const string NoFamilies = "У вас пока нет семей. Создайте свою первую семью!";

    public const string NoFamiliesJoin =
      "У вас пока нет семей. Создайте свою первую семью или присоединитесь к существующей.";

    public const string FamilyDeleted =
      "Все данные семьи, включая участников, спотов, задачи и статистику, были безвозвратно удалены.";

    public const string SpotDeleted =
      "Все шаблоны задач и задачи, связанные со спотом, были безвозвратно удалены.";

    public const string ConfirmFamilyDeletion = "Вы уверены, что хотите удалить эту семью?\n\n" +
                                                "⚠️ Это действие нельзя отменить!";

    public const string ConfirmSpotDeletion = "Вы уверены, что хотите удалить этого спота?\n\n" +
                                              "⚠️ Это действие нельзя отменить!";

    public const string ConfirmDeletion = "Подтвердите удаление:";
    public const string SendInviteLink = "Отправьте эту ссылку человеку, которого хотите пригласить в семью.";

    public const string CronExamples = "Примеры:\n" +
                                       "• `0 0 9 * * ?` - ежедневно в 9:00\n" +
                                       "• `0 0 20 * * ?` - ежедневно в 20:00\n" +
                                       "• `0 0 9 */5 * ?` - каждые 5 дней в 9:00\n" +
                                       "• `0 0 9 * * MON` - каждый понедельник в 9:00";

    public const string SendLocation = "Нажмите \"📍 Отправить местоположение\" для автоматического определения, " +
                                       "или введите название города вручную.";

    public const string SpotTasksAvailable =
      "Теперь вы можете создавать задачи для ухода за спотом через меню \"🧩 Споты\".\n\n";

    public const string TaskAvailableToAll = "Задача доступна всем участникам семьи.";
    public const string ScheduledTask = "Задача будет автоматически создаваться по расписанию.";

    public const string LeaderboardDisabled = "Лидерборд отключён в настройках семьи.\n\n" +
                                              "Администратор может включить его в настройках.";

    public const string NoActiveTasks =
      "📋 Активных задач пока нет.\n\nЗадачи можно создавать из шаблонов задач меню спота.";

    public const string OrBackToManual = "или \"⬅️ Назад\" для выбора вручную.";
    public const string DefaultFamilyName = "ваша семья";

    public static string ChooseTimezoneMethod(string familyName) =>
      $"🌍 Выберите способ определения временной зоны для семьи \"{familyName}\":";

    public static string FamilyCreatedWithTimezone(string familyName, string timezone) =>
      Success.FamilyCreatedMessage(familyName) +
      $"🌍 Определенная временная зона: {timezone}\n\n" +
      Success.NextStepsMessage;

    public static string GetTaskTySpotext(string taskType) => taskType == "onetime" ? "разовую" : "периодическую";

    public static string FamilyJoined(string familyName, string roleName) =>
      $"Вы успешно присоединились к семье *{familyName}*\n" +
      $"Ваша роль: {roleName}\n\n";
  }

  public static class Help
  {
    public static string Message(string chatUrl) => $@"🚀 Chore Heroes — бот, который помогает семье распределять дела.

*3 уровня в боте:*
- *Спот* → *Шаблоны задач* → *Задачи*

*🧩 Спот*
Спот — это объект/место/тема, к которой относятся дела.
Примеры: ""Кухня"", ""Ванная"", ""Кот"", ""Уроки"", ""Мусор"", и даже член семьи.

*📋 Шаблон задачи*
Шаблон — это правило внутри спота: *что делать*, *сколько очков*, *как часто создавать*, *за сколько нужно успеть*.
Примеры (внутри спота ""Кухня""):
- ""Помыть посуду"" — ежедневно 20:00, срок 12 часов.
- ""Протереть стол"" — по будням 19:00, срок 6 часов.

*✅ Задача*
Задача (или миссия) появляется из шаблона *по расписанию* или *вручную*.
Её можно взять в работу в ✅ Мои задачи и отметить выполнение.

*✨ Быстрый старт*
1. /start — зарегистрируйтесь.
2. Создайте или выберите 🏠 Семью.
3. Пригласите других членов семьи.
4. Создайте 🧩 Спот (например: ""Кухня"").
5. Добавьте 📋 Шаблоны задач для этого спота (расписание или ручной запуск).
6. Дальше бот сам будет создавать ✅ задачи по расписанию, а вы будете их выполнять.

*🔁 Как назначается ответственный*
Если в шаблоне указаны ответственные — задача или миссия будет назначаться им по очереди.
Если нет — бот выберет ответственного из участников спота.
Если и там нет ответственных, тогда миссия будет ждать того, кто возьмёт на себя её выполнение. 

*🌟 Типичные сценарии*
1. *Уборка по расписанию*:
  создали спот ""Кухня"" → шаблон ""Мыть посуду"" ежедневно → задачи появляются сами.
2. *Разовая срочная задача*:
  открыли спот → выбрали шаблон → создали задачу вручную → кто-то взял в ✅ Мои задачи.
3. *Очередность*:
  указали ответственных (в споте или в шаблоне) → бот по кругу назначает задачи, чтобы было честно.
4. *Контроль сроков*:
  в шаблоне задали срок (например 12 часов) → у задач будет дедлайн.

*👨‍👩‍👧‍👦 Роли в семье*
В каждой семье у участника есть роль: *Админ*, *Взрослый* или *Ребёнок*.

*Админ* — управляет семьёй:
- создаёт приглашения
- меняет роли участников
- удаляет участников
- может удалить спот или всю семью

*Взрослый* и *Ребёнок* — выполняют задачи:
- берут задачи в ✅ Мои задачи
- отмечают выполнение
- получают очки и попадают в статистику

*Ребёнок* не может менять список ответственных в спотах и шаблонах (видит только список).
*Взрослые* и *админы* могут назначать/снимать ответственных.

💬 [Оставить идею или предложение]({chatUrl})
";
  }

  public static class Templates
  {
    public const string NoTemplates = "📋 У этого спота пока нет шаблонов задач.";
    public const string TemplateCreated = "✅ Шаблон задачи успешно создан!";
    public const string TemplateUpdated = "✅ Шаблон задачи успешно обновлён!";
    public const string TemplateDeleted = "✅ Шаблон задачи успешно удалён!";
    public const string EnterTemplatePoints = "⭐ Выберите сложность задачи:";

    public const string ChooseScheduleType = "🔄 Выберите тип расписания для шаблона задачи:";
    public const string EnterScheduleTime = "⏰ Введите время в формате HH:mm (например, 09:00 или 15:30):";
    public const string ChooseWeekday = "📅 Выберите день недели:";
    public const string EnterMonthDay = "📅 Введите день месяца (от 1 до 31):";

    public static readonly string EnterTemplateTitle =
      $"📝 Введите название шаблона задачи (от {TaskTitle.MinLength} до {TaskTitle.MaxLength} символов):";

    public static readonly string EnterDueDuration =
      $"⏰ Введите срок выполнения задачи в часах (от {DueDuration.MinHours} до {DueDuration.MaxHours}, где 24 = 1 день, 720 = 30 дней):";
  }
}
