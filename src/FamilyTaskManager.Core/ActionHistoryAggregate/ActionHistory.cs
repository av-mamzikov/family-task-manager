namespace FamilyTaskManager.Core.ActionHistoryAggregate;

public enum ActionType
{
  TaskCreated,
  TaskCompleted,
  MemberAdded,
  PetMoodUpdated,
  FamilyCreated
}

public class ActionHistory : EntityBase<ActionHistory, Guid>, IAggregateRoot
{
  private ActionHistory() { }

  public ActionHistory(Guid familyId, Guid userId, ActionType actionType, string description, string? metadataJson)
  {
    Guard.Against.Default(familyId);
    Guard.Against.Default(userId);
    Guard.Against.NullOrWhiteSpace(description);

    FamilyId = familyId;
    UserId = userId;
    ActionType = actionType;
    Description = description.Trim();
    MetadataJson = metadataJson;
    CreatedAt = DateTime.UtcNow;
  }

  public Guid FamilyId { get; private set; }
  public Guid UserId { get; private set; }
  public ActionType ActionType { get; private set; }
  public string Description { get; private set; } = null!;
  public string? MetadataJson { get; private set; }
  public DateTime CreatedAt { get; private set; }
}
