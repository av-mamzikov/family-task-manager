using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.UnitTests.Helpers;

namespace FamilyTaskManager.UnitTests.Core.TaskAggregate;

public class TaskTemplateTests
{
  private readonly Guid _familyId = Guid.NewGuid();
  private readonly Guid _spotId = Guid.NewGuid();
  private readonly Guid _createdBy = Guid.NewGuid();
  private readonly TaskTitle _title = new("Test Task");
  private readonly TaskPoints _points = new(3);
  private readonly Schedule _schedule;
  private readonly DueDuration _dueDuration = new(TimeSpan.FromHours(2));

  public TaskTemplateTests()
  {
    _schedule = Schedule.CreateDaily(new TimeOnly(9, 0)).Value;
  }

  [Fact]
  public void Constructor_WithValidParameters_CreatesTaskTemplate()
  {
    // Act
    var taskTemplate = new TaskTemplate(_familyId, _spotId, _title, _points, _schedule, _dueDuration, _createdBy);

    // Assert
    taskTemplate.FamilyId.ShouldBe(_familyId);
    taskTemplate.SpotId.ShouldBe(_spotId);
    taskTemplate.Title.ShouldBe(_title);
    taskTemplate.Points.ShouldBe(_points);
    taskTemplate.Schedule.ShouldBe(_schedule);
    taskTemplate.DueDuration.ShouldBe(_dueDuration);
    taskTemplate.CreatedBy.ShouldBe(_createdBy);
    taskTemplate.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
    taskTemplate.ResponsibleMembers.ShouldBeEmpty();
  }

  [Fact]
  public void Constructor_WithEmptyFamilyId_ThrowsException()
  {
    // Arrange
    var emptyFamilyId = Guid.Empty;

    // Act & Assert
    Should.Throw<ArgumentException>(() => 
      new TaskTemplate(emptyFamilyId, _spotId, _title, _points, _schedule, _dueDuration, _createdBy));
  }

  [Fact]
  public void Constructor_WithEmptySpotId_ThrowsException()
  {
    // Arrange
    var emptySpotId = Guid.Empty;

    // Act & Assert
    Should.Throw<ArgumentException>(() => 
      new TaskTemplate(_familyId, emptySpotId, _title, _points, _schedule, _dueDuration, _createdBy));
  }

  [Fact]
  public void Constructor_WithEmptyCreatedBy_ThrowsException()
  {
    // Arrange
    var emptyCreatedBy = Guid.Empty;

    // Act & Assert
    Should.Throw<ArgumentException>(() => 
      new TaskTemplate(_familyId, _spotId, _title, _points, _schedule, _dueDuration, emptyCreatedBy));
  }

  [Fact]
  public void Constructor_WithNullTitle_ThrowsException()
  {
    // Act & Assert
    Should.Throw<ArgumentException>(() => 
      new TaskTemplate(_familyId, _spotId, null!, _points, _schedule, _dueDuration, _createdBy));
  }

  [Fact]
  public void Constructor_WithNullPoints_ThrowsException()
  {
    // Act & Assert
    Should.Throw<ArgumentException>(() => 
      new TaskTemplate(_familyId, _spotId, _title, null!, _schedule, _dueDuration, _createdBy));
  }

  [Fact]
  public void Constructor_WithNullSchedule_ThrowsException()
  {
    // Act & Assert
    Should.Throw<ArgumentException>(() => 
      new TaskTemplate(_familyId, _spotId, _title, _points, null!, _dueDuration, _createdBy));
  }

  [Fact]
  public void Constructor_WithNullDueDuration_ThrowsException()
  {
    // Act & Assert
    Should.Throw<ArgumentException>(() => 
      new TaskTemplate(_familyId, _spotId, _title, _points, _schedule, null!, _createdBy));
  }

  [Fact]
  public void Update_WithValidParameters_UpdatesProperties()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(_familyId, _spotId, _title, _points, _schedule, _dueDuration, _createdBy);
    var newTitle = new TaskTitle("Updated Task");
    var newPoints = new TaskPoints(4);
    var newSchedule = Schedule.CreateWeekly(new TimeOnly(14, 0), DayOfWeek.Monday).Value;
    var newDueDuration = new DueDuration(TimeSpan.FromHours(4));

    // Act
    taskTemplate.Update(newTitle, newPoints, newSchedule, newDueDuration);

    // Assert
    taskTemplate.Title.ShouldBe(newTitle);
    taskTemplate.Points.ShouldBe(newPoints);
    taskTemplate.Schedule.ShouldBe(newSchedule);
    taskTemplate.DueDuration.ShouldBe(newDueDuration);
  }

  [Fact]
  public void Update_WithNullTitle_ThrowsException()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(_familyId, _spotId, _title, _points, _schedule, _dueDuration, _createdBy);

    // Act & Assert
    Should.Throw<ArgumentException>(() => 
      taskTemplate.Update(null!, _points, _schedule, _dueDuration));
  }

  [Fact]
  public void Update_WithNullPoints_ThrowsException()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(_familyId, _spotId, _title, _points, _schedule, _dueDuration, _createdBy);

    // Act & Assert
    Should.Throw<ArgumentException>(() => 
      taskTemplate.Update(_title, null!, _schedule, _dueDuration));
  }

  [Fact]
  public void Update_WithNullSchedule_ThrowsException()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(_familyId, _spotId, _title, _points, _schedule, _dueDuration, _createdBy);

    // Act & Assert
    Should.Throw<ArgumentException>(() => 
      taskTemplate.Update(_title, _points, null!, _dueDuration));
  }

  [Fact]
  public void Update_WithNullDueDuration_ThrowsException()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(_familyId, _spotId, _title, _points, _schedule, _dueDuration, _createdBy);

    // Act & Assert
    Should.Throw<ArgumentException>(() => 
      taskTemplate.Update(_title, _points, _schedule, null!));
  }

  [Fact]
  public void AssignResponsible_WithValidMember_AddsMemberToResponsibleCollection()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(_familyId, _spotId, _title, _points, _schedule, _dueDuration, _createdBy);
    var member = TestHelpers.CreateMemberWithUser();
    typeof(FamilyMember).GetProperty("FamilyId")!.SetValue(member, _familyId);

    // Act
    taskTemplate.AssignResponsible(member);

    // Assert
    taskTemplate.ResponsibleMembers.ShouldContain(m => m.Id == member.Id);
  }

  [Fact]
  public void AssignResponsible_SameMemberTwice_DoesNotCreateDuplicates()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(_familyId, _spotId, _title, _points, _schedule, _dueDuration, _createdBy);
    var member = TestHelpers.CreateMemberWithUser();
    typeof(FamilyMember).GetProperty("FamilyId")!.SetValue(member, _familyId);

    // Act
    taskTemplate.AssignResponsible(member);
    taskTemplate.AssignResponsible(member);

    // Assert
    taskTemplate.ResponsibleMembers.Count(m => m.Id == member.Id).ShouldBe(1);
  }

  [Fact]
  public void AssignResponsible_MemberFromAnotherFamily_ThrowsException()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(_familyId, _spotId, _title, _points, _schedule, _dueDuration, _createdBy);
    var member = TestHelpers.CreateMemberWithUser();
    typeof(FamilyMember).GetProperty("FamilyId")!.SetValue(member, Guid.NewGuid());

    // Act & Assert
    Should.Throw<ArgumentException>(() => taskTemplate.AssignResponsible(member));
  }

  [Fact]
  public void AssignResponsible_InactiveMember_ThrowsException()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(_familyId, _spotId, _title, _points, _schedule, _dueDuration, _createdBy);
    var member = TestHelpers.CreateMemberWithUser();
    typeof(FamilyMember).GetProperty("FamilyId")!.SetValue(member, _familyId);
    typeof(FamilyMember).GetProperty("IsActive")!.SetValue(member, false);

    // Act & Assert
    Should.Throw<InvalidOperationException>(() => taskTemplate.AssignResponsible(member));
  }

  [Fact]
  public void RemoveResponsible_WhenMemberAssigned_RemovesFromCollection()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(_familyId, _spotId, _title, _points, _schedule, _dueDuration, _createdBy);
    var member = TestHelpers.CreateMemberWithUser();
    typeof(FamilyMember).GetProperty("FamilyId")!.SetValue(member, _familyId);
    taskTemplate.AssignResponsible(member);

    // Act
    taskTemplate.RemoveResponsible(member);

    // Assert
    taskTemplate.ResponsibleMembers.ShouldNotContain(m => m.Id == member.Id);
  }

  [Fact]
  public void RemoveResponsible_WhenMemberNotAssigned_DoesNothing()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(_familyId, _spotId, _title, _points, _schedule, _dueDuration, _createdBy);
    var member = TestHelpers.CreateMemberWithUser();
    var initialCount = taskTemplate.ResponsibleMembers.Count;

    // Act
    taskTemplate.RemoveResponsible(member);

    // Assert
    taskTemplate.ResponsibleMembers.Count.ShouldBe(initialCount);
  }

  [Fact]
  public void ClearAllResponsible_WhenMembersAssigned_ClearsAllMembers()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(_familyId, _spotId, _title, _points, _schedule, _dueDuration, _createdBy);
    var member1 = TestHelpers.CreateMemberWithUser("User1");
    var member2 = TestHelpers.CreateMemberWithUser("User2");
    typeof(FamilyMember).GetProperty("FamilyId")!.SetValue(member1, _familyId);
    typeof(FamilyMember).GetProperty("FamilyId")!.SetValue(member2, _familyId);

    taskTemplate.AssignResponsible(member1);
    taskTemplate.AssignResponsible(member2);

    // Act
    taskTemplate.ClearAllResponsible();

    // Assert
    taskTemplate.ResponsibleMembers.ShouldBeEmpty();
  }

  [Fact]
  public void ClearAllResponsible_WhenNoMembersAssigned_DoesNothing()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(_familyId, _spotId, _title, _points, _schedule, _dueDuration, _createdBy);
    var initialCount = taskTemplate.ResponsibleMembers.Count;

    // Act
    taskTemplate.ClearAllResponsible();

    // Assert
    taskTemplate.ResponsibleMembers.Count.ShouldBe(initialCount);
  }

  [Fact]
  public void ResponsibleMembers_IsReadOnlyCollection()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(_familyId, _spotId, _title, _points, _schedule, _dueDuration, _createdBy);
    var member = TestHelpers.CreateMemberWithUser();
    typeof(FamilyMember).GetProperty("FamilyId")!.SetValue(member, _familyId);
    taskTemplate.AssignResponsible(member);

    // Act & Assert
    var responsibleMembers = taskTemplate.ResponsibleMembers;
    responsibleMembers.ShouldNotBeNull();
    responsibleMembers.ShouldBeAssignableTo<IReadOnlyCollection<FamilyMember>>();
  }

  [Fact]
  public void MultipleOperations_WithResponsibleMembers_WorksCorrectly()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(_familyId, _spotId, _title, _points, _schedule, _dueDuration, _createdBy);
    var member1 = TestHelpers.CreateMemberWithUser("User1");
    var member2 = TestHelpers.CreateMemberWithUser("User2");
    var member3 = TestHelpers.CreateMemberWithUser("User3");
    typeof(FamilyMember).GetProperty("FamilyId")!.SetValue(member1, _familyId);
    typeof(FamilyMember).GetProperty("FamilyId")!.SetValue(member2, _familyId);
    typeof(FamilyMember).GetProperty("FamilyId")!.SetValue(member3, _familyId);

    // Act - Add three members
    taskTemplate.AssignResponsible(member1);
    taskTemplate.AssignResponsible(member2);
    taskTemplate.AssignResponsible(member3);

    // Assert - All three members added
    taskTemplate.ResponsibleMembers.Count.ShouldBe(3);
    taskTemplate.ResponsibleMembers.ShouldContain(m => m.Id == member1.Id);
    taskTemplate.ResponsibleMembers.ShouldContain(m => m.Id == member2.Id);
    taskTemplate.ResponsibleMembers.ShouldContain(m => m.Id == member3.Id);

    // Act - Remove one member
    taskTemplate.RemoveResponsible(member2);

    // Assert - Only two members remain
    taskTemplate.ResponsibleMembers.Count.ShouldBe(2);
    taskTemplate.ResponsibleMembers.ShouldContain(m => m.Id == member1.Id);
    taskTemplate.ResponsibleMembers.ShouldNotContain(m => m.Id == member2.Id);
    taskTemplate.ResponsibleMembers.ShouldContain(m => m.Id == member3.Id);

    // Act - Clear all
    taskTemplate.ClearAllResponsible();

    // Assert - No members remain
    taskTemplate.ResponsibleMembers.ShouldBeEmpty();
  }
}