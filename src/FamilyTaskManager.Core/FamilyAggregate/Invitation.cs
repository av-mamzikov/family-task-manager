using FamilyTaskManager.Core.UserAggregate;

namespace FamilyTaskManager.Core.FamilyAggregate;

public class Invitation : EntityBase<Invitation, Guid>, IAggregateRoot
{
  private Invitation()
  {
  }

  public Invitation(Guid familyId, FamilyRole role, Guid createdBy, int expirationDays = 7)
  {
    Guard.Against.Default(familyId);
    Guard.Against.Default(createdBy);

    Id = Guid.NewGuid();
    FamilyId = familyId;
    Role = role;
    CreatedBy = createdBy;
    Code = GenerateCode();
    CreatedAt = DateTime.UtcNow;
    ExpiresAt = expirationDays != 0 ? DateTime.UtcNow.AddDays(expirationDays) : null;
    IsActive = true;
  }

  public Guid FamilyId { get; private set; }
  public FamilyRole Role { get; private set; }
  public string Code { get; private set; } = null!;
  public DateTime CreatedAt { get; private set; }
  public DateTime? ExpiresAt { get; }
  public bool IsActive { get; private set; }
  public Guid CreatedBy { get; private set; }

  // Navigation properties
  public Family Family { get; private set; } = null!;
  public User Creator { get; private set; } = null!;

  public void Deactivate() => IsActive = false;

  public bool IsExpired() => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

  public bool IsValid() => IsActive && !IsExpired();

  private static string GenerateCode()
  {
    // Generate a unique 8-character alphanumeric code
    const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Removed ambiguous characters
    var random = new Random();
    return new string(Enumerable.Range(0, 8)
      .Select(_ => chars[random.Next(chars.Length)])
      .ToArray());
  }
}
