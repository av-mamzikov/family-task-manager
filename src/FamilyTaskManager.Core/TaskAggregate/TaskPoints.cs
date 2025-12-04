namespace FamilyTaskManager.Core.TaskAggregate;

/// <summary>
///   Value Object representing task points (1-3).
///   Encapsulates points validation and display logic.
/// </summary>
public class TaskPoints : ValueObject
{
  public const int MinValue = 1;
  public const int MaxValue = 4;

  private TaskPoints()
  {
    // EF Core
  }

  public TaskPoints(int value)
  {
    Guard.Against.OutOfRange(value, nameof(value), MinValue, MaxValue);
    Value = value;
  }

  public static TaskPoints Easy { get; } = new(1);
  public static TaskPoints Medium { get; } = new(2);
  public static TaskPoints Hard { get; } = new(3);
  public static TaskPoints VeryHard { get; } = new(4);

  public int Value { get; }

  /// <summary>
  ///   Returns star representation of points (⭐, ⭐⭐, ⭐⭐⭐).
  /// </summary>
  public string ToStars() => new('⭐', Value);

  /// <summary>
  ///   Implicit conversion to int for backward compatibility.
  /// </summary>
  public static implicit operator int(TaskPoints points) => points.Value;

  protected override IEnumerable<object> GetEqualityComponents()
  {
    yield return Value;
  }


  public override string ToString() => ToStars();
}
