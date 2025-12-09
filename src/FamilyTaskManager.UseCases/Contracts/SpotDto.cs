namespace FamilyTaskManager.UseCases.Contracts;

public record SpotDto(Guid Id, Guid FamilyId, string Name, SpotType Type, int MoodScore);
