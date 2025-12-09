namespace FamilyTaskManager.UseCases.Contracts;

public record LeaderboardEntryDto(Guid UserId, string UserName, int Points, FamilyRole Role);
