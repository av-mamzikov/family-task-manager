namespace FamilyTaskManager.UseCases.Features.TasksManagement.Dtos;

public record TaskReminderDto(
  Guid TaskId,
  Guid FamilyId,
  string Title,
  DateTime DueAt,
  List<Guid> FamilyMemberIds);
