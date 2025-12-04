namespace FamilyTaskManager.UseCases.Contracts.Spots;

public record SpotDto(Guid Id, Guid FamilyId, string Name, SpotType Type, int MoodScore);
