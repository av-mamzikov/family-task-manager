namespace FamilyTaskManager.UseCases.Contracts.Pets;

public record PetDto(Guid Id, Guid FamilyId, string Name, PetType Type, int MoodScore);
