namespace FamilyTaskManager.UseCases.Contracts.Statistics;

public record LeaderboardEntryDto(Guid UserId, string UserName, int Points, FamilyRole Role);
