namespace FamilyTaskManager.UseCases.Features.Statistics.Dtos;

public record LeaderboardEntryDto(Guid UserId, string UserName, int Points, FamilyRole Role);
