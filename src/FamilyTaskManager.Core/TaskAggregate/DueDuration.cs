namespace FamilyTaskManager.Core.TaskAggregate;

public sealed class DueDuration : ValueObject
{
  public const double MinHours = 1;
  public const double MaxHours = 720; // 30 days

  private DueDuration()
  {
    // EF Core
  }

  public DueDuration(TimeSpan value)
  {
    Guard.Against.OutOfRange(value.TotalHours, nameof(value), MinHours, MaxHours);
    Value = value;
  }

  public TimeSpan Value { get; }

  public double TotalHours => Value.TotalHours;

  public static DueDuration FromHours(double hours) => new(TimeSpan.FromHours(hours));

  public static bool IsValidHours(double hours) => hours is >= MinHours and <= MaxHours;

  public static implicit operator TimeSpan(DueDuration duration) => duration.Value;

  protected override IEnumerable<object> GetEqualityComponents()
  {
    yield return Value;
  }

  public override string ToString() => $"{TotalHours} часов";
}
