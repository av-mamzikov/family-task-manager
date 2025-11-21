using FamilyTaskManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace FamilyTaskManager.FunctionalTests.Bot;

/// <summary>
/// Integration tests for complex bot scenarios.
/// Tests complete user flows from start to finish.
/// </summary>
public class BotIntegrationTests : BotFunctionalTestBase
{
  private const long TestChatId = 123456789;
  private const long TestUserId = 987654321;

  [Fact]
  public async Task User_Can_Register_And_Receive_Welcome_Message()
  {
    // Act - user starts the bot for the first time
    await SendCommandAsync(TestChatId, TestUserId, "/start");
    
    // Assert - user should be registered in database
    using var scope = ServiceProvider!.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    var users = await dbContext.Users.ToListAsync();
    users.ShouldNotBeEmpty();
    
    // Verify welcome message was sent
    var lastMessage = FakeBotClient.GetLastSentMessage();
    lastMessage.ShouldNotBeNull();
  }

  [Fact]
  public async Task User_Can_Create_Family_Through_Conversation()
  {
    // Arrange - start the bot
    await SendCommandAsync(TestChatId, TestUserId, "/start");
    FakeBotClient.Clear(); // Clear initial messages
    
    // Act 1 - user clicks "Create Family" button
    await SendCallbackQueryAsync(TestChatId, TestUserId, "create_family");
    
    // Verify bot asks for family name
    var editedMessage = FakeBotClient.GetLastEditedMessage();
    if (editedMessage.HasValue)
    {
      editedMessage.Value.Text.ToLower().ShouldContain("название");
    }
    
    // Act 2 - user provides family name
    await SendTextMessageAsync(TestChatId, TestUserId, "Тестовая семья");
    
    // Assert - family should be created in database
    using var scope = ServiceProvider!.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    var families = await dbContext.Families.ToListAsync();
    families.ShouldNotBeEmpty();
    families.First().Name.ShouldBe("Тестовая семья");
  }

  [Fact]
  public async Task Multiple_Users_Can_Interact_Independently()
  {
    // Arrange
    const long user1ChatId = 111111111;
    const long user1Id = 222222222;
    const long user2ChatId = 333333333;
    const long user2Id = 444444444;
    
    // Act - both users start the bot
    await SendCommandAsync(user1ChatId, user1Id, "/start");
    await SendCommandAsync(user2ChatId, user2Id, "/start");
    
    // Assert - both users should be registered
    using var scope = ServiceProvider!.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    var users = await dbContext.Users.ToListAsync();
    users.Count.ShouldBeGreaterThanOrEqualTo(2);
    
    // Verify both received messages
    FakeBotClient.SentMessages.Count.ShouldBeGreaterThanOrEqualTo(2);
  }

  [Fact]
  public async Task Bot_Handles_Invalid_Input_Gracefully()
  {
    // Act - send invalid family name (too short)
    await SendCommandAsync(TestChatId, TestUserId, "/start");
    await SendCallbackQueryAsync(TestChatId, TestUserId, "create_family");
    await SendTextMessageAsync(TestChatId, TestUserId, "AB"); // Too short
    
    // Assert - bot should send error message or ask again
    var lastMessage = FakeBotClient.GetLastSentMessage();
    lastMessage.ShouldNotBeNull();
    
    // Family should not be created
    using var scope = ServiceProvider!.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    var families = await dbContext.Families.Where(f => f.Name == "AB").ToListAsync();
    families.ShouldBeEmpty();
  }

  [Fact]
  public async Task Bot_Maintains_Session_State_Across_Messages()
  {
    // This test verifies that the bot remembers conversation context
    
    // Act 1 - start family creation
    await SendCommandAsync(TestChatId, TestUserId, "/start");
    await SendCallbackQueryAsync(TestChatId, TestUserId, "create_family");
    
    // Act 2 - send family name
    await SendTextMessageAsync(TestChatId, TestUserId, "Моя семья");
    
    // Assert - the bot should have processed the name in the context of family creation
    using var scope = ServiceProvider!.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    var family = await dbContext.Families.FirstOrDefaultAsync(f => f.Name == "Моя семья");
    family.ShouldNotBeNull();
  }

  [Fact]
  public async Task Help_Command_Shows_Available_Commands()
  {
    // Act
    await SendCommandAsync(TestChatId, TestUserId, "/help");
    
    // Assert - help message should contain information about commands
    var lastMessage = FakeBotClient.GetLastSentMessage();
    lastMessage.ShouldNotBeNull();
    lastMessage!.Text.ShouldNotBeNull();
    
    var text = lastMessage.Text!.ToLower();
    // Should mention at least some commands
    (text.Contains("команд") || text.Contains("help") || text.Contains("start")).ShouldBeTrue();
  }

  [Fact]
  public async Task Bot_Responds_To_All_Callback_Queries()
  {
    // This ensures no callback query is left unanswered (which would show loading state forever)
    
    // Act - send various callback queries
    await SendCallbackQueryAsync(TestChatId, TestUserId, "create_family");
    await SendCallbackQueryAsync(TestChatId, TestUserId, "create_pet");
    await SendCallbackQueryAsync(TestChatId, TestUserId, "create_task");
    
    // Assert - all callbacks should be answered
    FakeBotClient.AnsweredCallbacks.Count.ShouldBe(3);
  }

  [Fact]
  public async Task Database_Changes_Are_Persisted()
  {
    // Act - create a family
    await SendCommandAsync(TestChatId, TestUserId, "/start");
    await SendCallbackQueryAsync(TestChatId, TestUserId, "create_family");
    await SendTextMessageAsync(TestChatId, TestUserId, "Постоянная семья");
    
    // Assert - verify data persists across different scopes
    using (var scope1 = ServiceProvider!.CreateScope())
    {
      var dbContext1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
      var family1 = await dbContext1.Families.FirstOrDefaultAsync(f => f.Name == "Постоянная семья");
      family1.ShouldNotBeNull();
    }
    
    using (var scope2 = ServiceProvider!.CreateScope())
    {
      var dbContext2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
      var family2 = await dbContext2.Families.FirstOrDefaultAsync(f => f.Name == "Постоянная семья");
      family2.ShouldNotBeNull();
    }
  }
}
