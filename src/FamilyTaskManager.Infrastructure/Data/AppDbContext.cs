using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.Infrastructure.Notifications;
using TaskStatus = FamilyTaskManager.Core.TaskAggregate.TaskStatus;

namespace FamilyTaskManager.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
  public DbSet<User> Users => Set<User>();
  public DbSet<Family> Families => Set<Family>();
  public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
  public DbSet<Invitation> Invitations => Set<Invitation>();
  public DbSet<Pet> Pets => Set<Pet>();
  public DbSet<TaskTemplate> TaskTemplates => Set<TaskTemplate>();
  public DbSet<TaskInstance> TaskInstances => Set<TaskInstance>();
  public DbSet<DomainEventOutbox> DomainEventOutbox => Set<DomainEventOutbox>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

    modelBuilder.Entity<Pet>()
      .HasQueryFilter(p => !p.IsDeleted);

    modelBuilder.Entity<TaskTemplate>()
      .HasQueryFilter(t => !t.Pet.IsDeleted);

    // Для удалённого питомца показываем только завершённые задачи
    modelBuilder.Entity<TaskInstance>()
      .HasQueryFilter(t => t.Status != TaskStatus.Completed || !t.Pet.IsDeleted);
  }

  public override int SaveChanges() => SaveChangesAsync().GetAwaiter().GetResult();
}
