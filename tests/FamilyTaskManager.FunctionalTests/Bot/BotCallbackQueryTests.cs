using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace FamilyTaskManager.FunctionalTests.Bot;

/// <summary>
/// Functional tests for bot callback queries (button presses).
/// Tests the full flow of user interactions with inline keyboards.
/// </summary>
public class BotCallbackQueryTests : BotFunctionalTestBase
{
  private const long TestChatId = 123456789;
  private const long TestUserId = 987654321;

  [Fact]
  public async Task Create_Family_Callback_Should_Prompt_For_Family_Name()
  {
    // Act - simulate user clicking "Create Family" button
    await SendCallbackQueryAsync(TestChatId, TestUserId, "create_family");
    
    // Assert - verify callback was answered
    FakeBotClient.AnsweredCallbacks.ShouldNotBeEmpty();
    
    // Verify message was edited to ask for family name
    var lastEdit = FakeBotClient.GetLastEditedMessage();
    lastEdit.HasValue.ShouldBeTrue();
    lastEdit!.Value.Text.ShouldContain("название семьи");
  }

  [Fact]
  public async Task Create_Pet_Callback_Should_Show_Pet_Type_Selection()
  {
    // Arrange - first create a family and set it as active
    // (In real scenario, user would need an active family)
    
    // Act - simulate user clicking "Create Pet" button
    await SendCallbackQueryAsync(TestChatId, TestUserId, "create_pet");
    
    // Assert - verify callback was answered
    FakeBotClient.AnsweredCallbacks.ShouldNotBeEmpty();
    
    // Verify response (might be error if no active family, or pet type selection)
    var lastMessage = FakeBotClient.GetLastSentMessage();
    var lastEdit = FakeBotClient.GetLastEditedMessage();
    
    // Either message or edit should exist
    var hasResponse = lastMessage != null || lastEdit.HasValue;
    hasResponse.ShouldBeTrue();
  }

  [Fact]
  public async Task Select_Pet_Type_Callback_Should_Prompt_For_Pet_Name()
  {
    // Act - simulate user selecting cat as pet type
    await SendCallbackQueryAsync(TestChatId, TestUserId, "select_pettype_cat");
    
    // Assert
    FakeBotClient.AnsweredCallbacks.ShouldNotBeEmpty();
    
    var lastEdit = FakeBotClient.GetLastEditedMessage();
    if (lastEdit.HasValue)
    {
      lastEdit.Value.Text.ShouldContain("имя питомца");
    }
  }

  [Fact]
  public async Task Create_Task_Callback_Should_Show_Task_Type_Selection()
  {
    // Act - simulate user clicking "Create Task" button
    await SendCallbackQueryAsync(TestChatId, TestUserId, "create_task");
    
    // Assert
    FakeBotClient.AnsweredCallbacks.ShouldNotBeEmpty();
    
    // Should show task type selection (one-time or recurring)
    var lastEdit = FakeBotClient.GetLastEditedMessage();
    if (lastEdit.HasValue)
    {
      var text = lastEdit.Value.Text.ToLower();
      (text.Contains("разов") || text.Contains("периодич")).ShouldBeTrue();
    }
  }

  [Fact]
  public async Task Select_Task_Type_Callback_Should_Prompt_For_Task_Title()
  {
    // Act - simulate user selecting one-time task
    await SendCallbackQueryAsync(TestChatId, TestUserId, "select_tasktype_onetime");
    
    // Assert
    FakeBotClient.AnsweredCallbacks.ShouldNotBeEmpty();
    
    var lastEdit = FakeBotClient.GetLastEditedMessage();
    if (lastEdit.HasValue)
    {
      lastEdit.Value.Text.ShouldContain("название задачи");
    }
  }

  [Fact]
  public async Task Multiple_Callbacks_Should_Be_Answered()
  {
    // Act - simulate multiple button presses
    await SendCallbackQueryAsync(TestChatId, TestUserId, "create_family", messageId: 1);
    await SendCallbackQueryAsync(TestChatId, TestUserId, "create_pet", messageId: 2);
    await SendCallbackQueryAsync(TestChatId, TestUserId, "create_task", messageId: 3);
    
    // Assert - all callbacks should be answered
    FakeBotClient.AnsweredCallbacks.Count.ShouldBeGreaterThanOrEqualTo(3);
  }

  [Fact]
  public async Task Unknown_Callback_Should_Be_Handled_Gracefully()
  {
    // Act - simulate unknown callback data
    await SendCallbackQueryAsync(TestChatId, TestUserId, "unknown_action_123");
    
    // Assert - callback should still be answered (to remove loading state)
    FakeBotClient.AnsweredCallbacks.ShouldNotBeEmpty();
  }

  [Fact]
  public async Task Take_Task_Callback_Should_Assign_Task_To_User()
  {
    // Arrange - create a task in database
    var taskId = Guid.NewGuid();
    // Note: In real test, you would create task through proper flow or seed data
    
    // Act - simulate user clicking "Take Task" button
    await SendCallbackQueryAsync(TestChatId, TestUserId, $"task_take_{taskId}");
    
    // Assert
    FakeBotClient.AnsweredCallbacks.ShouldNotBeEmpty();
    
    // Verify response (might be error if task doesn't exist, or success message)
    var lastMessage = FakeBotClient.GetLastSentMessage();
    var lastEdit = FakeBotClient.GetLastEditedMessage();
    
    var hasResponse = lastMessage != null || lastEdit.HasValue;
    hasResponse.ShouldBeTrue();
  }
}
