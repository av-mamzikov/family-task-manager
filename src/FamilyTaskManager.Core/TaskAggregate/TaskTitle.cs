namespace FamilyTaskManager.Core.TaskAggregate;

public sealed class TaskTitle : ValueObject
{
  public const int MinLength = 3;
  public const int MaxLength = 100;

  private TaskTitle()
  {
    // EF Core
  }

  public TaskTitle(string value)
  {
    Guard.Against.NullOrWhiteSpace(value);

    var trimmed = value.Trim();

    if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
      throw new ArgumentOutOfRangeException(nameof(value),
        $"Название должно быть длиной от {MinLength} до {MaxLength} символов");

    Value = trimmed;
  }

  public string Value { get; } = null!;

  public static bool IsValid(string value)
  {
    if (string.IsNullOrWhiteSpace(value)) return false;
    var trimmed = value.Trim();
    return trimmed.Length is >= MinLength and <= MaxLength;
  }

  public static implicit operator string(TaskTitle title) => title.Value;

  protected override IEnumerable<object> GetEqualityComponents()
  {
    yield return Value;
  }

  public override string ToString() => Value;
}
