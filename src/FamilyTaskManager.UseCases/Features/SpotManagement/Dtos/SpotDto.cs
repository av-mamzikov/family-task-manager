namespace FamilyTaskManager.UseCases.Features.SpotManagement.Dtos;

public record SpotDto(Guid Id, Guid FamilyId, string Name, SpotType Type, int MoodScore);
