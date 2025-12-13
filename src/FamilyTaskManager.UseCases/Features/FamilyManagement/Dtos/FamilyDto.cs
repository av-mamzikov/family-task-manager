namespace FamilyTaskManager.UseCases.Features.FamilyManagement.Dtos;

public record FamilyDto(
  Guid Id,
  string Name,
  string Timezone,
  bool LeaderboardEnabled,
  FamilyRole UserRole,
  int UserPoints);
