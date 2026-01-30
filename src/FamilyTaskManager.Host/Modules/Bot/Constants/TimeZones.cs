namespace FamilyTaskManager.Host.Modules.Bot.Constants;

public static class TimeZones
{
  public static class Russian
  {
    // ReSharper disable MemberCanBePrivate.Global
    public const string Kaliningrad = "Europe/Kaliningrad";
    public const string Moscow = "Europe/Moscow";
    public const string Samara = "Europe/Samara";
    public const string Yekaterinburg = "Asia/Yekaterinburg";
    public const string Omsk = "Asia/Omsk";
    public const string Krasnoyarsk = "Asia/Krasnoyarsk";
    public const string Irkutsk = "Asia/Irkutsk";
    public const string Chita = "Asia/Chita";
    public const string Yakutsk = "Asia/Yakutsk";
    public const string Vladivostok = "Asia/Vladivostok";
    public const string Sakhalin = "Asia/Sakhalin";
    public const string Magadan = "Asia/Magadan";
    public const string Kamchatka = "Asia/Kamchatka";
    public const string Anadyr = "Asia/Anadyr";

    // ReSharper enable MemberCanBePrivate.Global

    public static readonly string[] All =
    [
      Kaliningrad,
      Moscow,
      Samara,
      Yekaterinburg,
      Omsk,
      Krasnoyarsk,
      Irkutsk,
      Chita,
      Yakutsk,
      Vladivostok,
      Sakhalin,
      Magadan,
      Kamchatka,
      Anadyr
    ];

    public static string GetName(string timezoneId) =>
      _names.TryGetValue(timezoneId, out var name)
        ? name
        : timezoneId;

    private static readonly Dictionary<string, string> _names = new()
    {
      { Kaliningrad, "Калининград" },
      { Moscow, "Москва" },
      { Samara, "Самара" },
      { Yekaterinburg, "Екатеринбург" },
      { Omsk, "Омск" },
      { Krasnoyarsk, "Красноярск" },
      { Irkutsk, "Иркутск" },
      { Chita, "Чита" },
      { Yakutsk, "Якутск" },
      { Vladivostok, "Владивосток" },
      { Sakhalin, "Сахалин" },
      { Magadan, "Магадан" },
      { Kamchatka, "Камчатка" },
      { Anadyr, "Анадырь" }
    };
  }
}
