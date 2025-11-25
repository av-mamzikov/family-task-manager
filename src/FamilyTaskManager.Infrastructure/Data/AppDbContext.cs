using FamilyTaskManager.Core.ActionHistoryAggregate;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Core.UserAggregate;

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
  public DbSet<ActionHistory> ActionHistory => Set<ActionHistory>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
  }

  public override int SaveChanges() => SaveChangesAsync().GetAwaiter().GetResult();
}
