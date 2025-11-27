using System.Text.RegularExpressions;
using FamilyTaskManager.FunctionalTests.Helpers;

namespace FamilyTaskManager.FunctionalTests.BotFlow.Family;

public class InviteUserBotFlowTests(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
  private const int BotProcessingDelayMs = 1000;

  public Task InitializeAsync()
  {
    factory.CreateClient();
    return Task.CompletedTask;
  }

  public Task DisposeAsync() => Task.CompletedTask;

  [Fact]
  public async Task TS_BOT_002_InviteUserViaStartCommand_ShouldJoinFamilyAndShowMainMenu()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Admin creates family via bot flow
    var adminTelegramId = TestDataBuilder.GenerateTelegramId();
    var adminChatId = adminTelegramId;
    var familyName = "Семья Петровых";

    // Step 1: start family creation via callback
    var createFamilyCallback = UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, "create_family");
    botClient.EnqueueUpdate(createFamilyCallback);
    await Task.Delay(BotProcessingDelayMs);

    // Step 2: enter family name
    var nameUpdate = UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, familyName);
    botClient.EnqueueUpdate(nameUpdate);
    await Task.Delay(BotProcessingDelayMs);

    // Step 3: show timezone list
    var showTimezoneList = UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, "timezone_showlist");
    botClient.EnqueueUpdate(showTimezoneList);
    await Task.Delay(BotProcessingDelayMs);

    // Step 4: select timezone
    var timezoneSelection = UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, "timezone_Europe/Moscow");
    botClient.EnqueueUpdate(timezoneSelection);
    await Task.Delay(BotProcessingDelayMs);

    // Now admin has a family and main menu is shown

    // Step 5: open family menu via /family
    var familyCommandUpdate = UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "/family");
    botClient.EnqueueUpdate(familyCommandUpdate);
    await Task.Delay(BotProcessingDelayMs);

    var familyMenuMessage = botClient.GetLastMessageTo(adminChatId)!;
    familyMenuMessage.ShouldContainText(familyName);
    var familyMenuKeyboard = familyMenuMessage.ShouldHaveInlineKeyboard();
    var createInviteButton = familyMenuKeyboard.GetButton("Создать приглашение");
    createInviteButton.CallbackData.ShouldNotBeNull();

    // Step 6: click "Create invite" button
    var createInviteCallback = UpdateFactory.CreateCallbackUpdate(
      adminChatId,
      adminTelegramId,
      createInviteButton.CallbackData!);
    botClient.EnqueueUpdate(createInviteCallback);
    await Task.Delay(BotProcessingDelayMs);

    // Step 7: select role for invite (Adult)
    var inviteRoleMessage = botClient.GetLastMessageTo(adminChatId)!;
    inviteRoleMessage.ShouldContainText("Создание приглашения");
    var inviteRoleKeyboard = inviteRoleMessage.ShouldHaveInlineKeyboard();
    var adultRoleButton = inviteRoleKeyboard.GetButton("Взрослый");
    adultRoleButton.CallbackData.ShouldNotBeNull();

    var selectRoleCallback = UpdateFactory.CreateCallbackUpdate(
      adminChatId,
      adminTelegramId,
      adultRoleButton.CallbackData!);
    botClient.EnqueueUpdate(selectRoleCallback);
    await Task.Delay(BotProcessingDelayMs);

    // Step 8: get invite link with payload
    var inviteMessage = botClient.GetLastMessageTo(adminChatId)!;
    inviteMessage.ShouldContainText("Приглашение создано");

    var inviteText = inviteMessage.Text!;
    var match = Regex.Match(inviteText, @"invite_[A-Z0-9]+");
    match.Success.ShouldBeTrue("Пригласительная ссылка должна содержать payload вида invite_CODE");
    var invitePayload = match.Value;

    // Act: invited user starts bot with invite payload
    var invitedTelegramId = TestDataBuilder.GenerateTelegramId();
    var invitedChatId = invitedTelegramId;

    botClient.Clear();

    var startWithInviteUpdate = UpdateFactory.CreateTextUpdate(
      invitedChatId,
      invitedTelegramId,
      $"/start {invitePayload}");
    botClient.EnqueueUpdate(startWithInviteUpdate);
    await Task.Delay(BotProcessingDelayMs);

    var invitedMessages = botClient.GetMessagesTo(invitedChatId).ToList();
    invitedMessages.ShouldNotBeEmpty();

    var welcomeMessage = invitedMessages.FirstOrDefault(m =>
      m.Text != null && m.Text.Contains("Добро пожаловать в семью"));
    welcomeMessage.ShouldNotBeNull("Должно быть приветственное сообщение о вступлении в семью");
    welcomeMessage!.Text!.ShouldContain(familyName);

    var mainMenuMessage = invitedMessages.Last();
    mainMenuMessage.ShouldContainText("Главное меню");
  }
}
