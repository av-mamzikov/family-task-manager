namespace FamilyTaskManager.Core.TaskAggregate;

public enum TaskType
{
  OneTime = 0,
  Recurring = 1
}

public enum TaskStatus
{
  Active = 0,
  InProgress = 1,
  Completed = 2
}
