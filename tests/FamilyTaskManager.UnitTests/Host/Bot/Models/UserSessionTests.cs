using FamilyTaskManager.Host.Modules.Bot.Models;

namespace FamilyTaskManager.UnitTests.Host.Bot.Models;

public class UserSessionTests
{
  [Fact]
  public void UpdateActivity_ShouldUpdateLastActivityTime()
  {
    // Arrange
    var session = new UserSession();
    var initialTime = session.LastActivity;
    Thread.Sleep(10); // Ensure time difference

    // Act
    session.UpdateActivity();

    // Assert
    session.LastActivity.ShouldBeGreaterThan(initialTime);
  }

  [Fact]
  public void SetState_ShouldUpdateStateAndActivity()
  {
    // Arrange
    var session = new UserSession();
    var initialTime = session.LastActivity;
    Thread.Sleep(10);

    // Act
    session.SetState(ConversationState.AwaitingFamilyName);

    // Assert
    session.State.ShouldBe(ConversationState.AwaitingFamilyName);
    session.LastActivity.ShouldBeGreaterThan(initialTime);
  }

  [Fact]
  public void SetState_ShouldUpdateDataWhenProvided()
  {
    // Arrange
    var session = new UserSession();
    var data = new Dictionary<string, object> { ["userId"] = Guid.NewGuid(), ["familyName"] = "Test Family" };

    // Act
    session.SetState(ConversationState.AwaitingFamilyName, data);

    // Assert
    session.State.ShouldBe(ConversationState.AwaitingFamilyName);
    session.Data.ShouldContainKey("userId");
    session.Data.ShouldContainKey("familyName");
    session.Data["familyName"].ShouldBe("Test Family");
  }

  [Fact]
  public void ClearState_ShouldResetStateAndData()
  {
    // Arrange
    var session = new UserSession();
    session.SetState(ConversationState.AwaitingPetName, new Dictionary<string, object> { ["petType"] = "cat" });

    // Act
    session.ClearState();

    // Assert
    session.State.ShouldBe(ConversationState.None);
    session.Data.ShouldBeEmpty();
  }

  [Fact]
  public void NewSession_ShouldHaveDefaultValues()
  {
    // Act
    var session = new UserSession();

    // Assert
    session.CurrentFamilyId.ShouldBeNull();
    session.State.ShouldBe(ConversationState.None);
    session.Data.ShouldNotBeNull();
    session.Data.ShouldBeEmpty();
    session.LastActivity.ShouldBeInRange(
      DateTime.UtcNow.AddSeconds(-1),
      DateTime.UtcNow.AddSeconds(1));
  }
}
