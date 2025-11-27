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
    await botClient.WaitForLastMessageToAsync(adminChatId, TimeSpan.FromSeconds(5));

    // Step 2: enter family name
    var nameUpdate = UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, familyName);
    botClient.EnqueueUpdate(nameUpdate);
    await botClient.WaitForLastMessageToAsync(adminChatId, TimeSpan.FromSeconds(5));

    // Step 3: show timezone list
    var showTimezoneList = UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, "timezone_showlist");
    botClient.EnqueueUpdate(showTimezoneList);
    await botClient.WaitForLastMessageToAsync(adminChatId, TimeSpan.FromSeconds(5));

    // Step 4: select timezone
    var timezoneSelection = UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, "timezone_Europe/Moscow");
    botClient.EnqueueUpdate(timezoneSelection);
    await botClient.WaitForLastMessageToAsync(adminChatId, TimeSpan.FromSeconds(5));

    // Now admin has a family and main menu is shown

    // Step 5: open family menu via /family
    var previousAdminMessagesCount = botClient.GetMessagesTo(adminChatId).Count();
    var familyCommandUpdate = UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "/family");
    botClient.EnqueueUpdate(familyCommandUpdate);

    var familyMessages =
      (await botClient.WaitForMessagesToAsync(adminChatId, previousAdminMessagesCount + 1, TimeSpan.FromSeconds(5)))
      .ToList();
    var familyMenuMessage = familyMessages.LastOrDefault();
    familyMenuMessage.ShouldNotBeNull("Бот должен показать меню семьи после команды /family");
    familyMenuMessage!.ShouldContainText(familyName);
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
    var roleMessages =
      (await botClient.WaitForMessagesToAsync(adminChatId, familyMessages.Count + 1, TimeSpan.FromSeconds(5))).ToList();
    var inviteRoleMessage = roleMessages.LastOrDefault();
    inviteRoleMessage.ShouldNotBeNull("Бот должен показать выбор роли для приглашения");
    inviteRoleMessage!.ShouldContainText("Создание приглашения");
    var inviteRoleKeyboard = inviteRoleMessage.ShouldHaveInlineKeyboard();
    var adultRoleButton = inviteRoleKeyboard.GetButton("Взрослый");
    adultRoleButton.CallbackData.ShouldNotBeNull();

    var selectRoleCallback = UpdateFactory.CreateCallbackUpdate(
      adminChatId,
      adminTelegramId,
      adultRoleButton.CallbackData!);
    botClient.EnqueueUpdate(selectRoleCallback);

    // Step 8: get invite link with payload
    var inviteMessages =
      (await botClient.WaitForMessagesToAsync(adminChatId, roleMessages.Count + 1, TimeSpan.FromSeconds(5))).ToList();
    var inviteMessage = inviteMessages.LastOrDefault();
    inviteMessage.ShouldNotBeNull("Бот должен отправить сообщение о создании приглашения");
    inviteMessage!.ShouldContainText("Приглашение создано");

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

    var invitedMessages = (await botClient.WaitForMessagesToAsync(invitedChatId, 2, TimeSpan.FromSeconds(5))).ToList();
    invitedMessages.ShouldNotBeEmpty();

    var welcomeMessage = invitedMessages.FirstOrDefault(m =>
      m.Text != null && m.Text.Contains("Добро пожаловать в семью"));
    welcomeMessage.ShouldNotBeNull("Должно быть приветственное сообщение о вступлении в семью");
    welcomeMessage!.Text!.ShouldContain(familyName);

    var mainMenuMessage = invitedMessages.Last();
    mainMenuMessage.ShouldContainText("Главное меню");
  }
}
