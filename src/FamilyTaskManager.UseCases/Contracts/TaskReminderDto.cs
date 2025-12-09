namespace FamilyTaskManager.UseCases.Contracts;

public record TaskReminderDto(
  Guid TaskId,
  Guid FamilyId,
  string Title,
  DateTime DueAt,
  List<Guid> FamilyMemberIds);
