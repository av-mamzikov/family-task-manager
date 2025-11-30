using System.Text.RegularExpressions;
using FamilyTaskManager.FunctionalTests.Helpers;

namespace FamilyTaskManager.FunctionalTests.BotFlow.Family;

public partial class InviteUserBotFlowTests(CustomWebApplicationFactory<Program> factory)
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
    await botClient.WaitForLastMessageToAsync(adminChatId);

    // Step 2: enter family name
    var nameUpdate = UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, familyName);
    botClient.EnqueueUpdate(nameUpdate);
    await botClient.WaitForLastMessageToAsync(adminChatId);

    // Step 3: show timezone list
    var showTimezoneList = UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, "timezone_showlist");
    botClient.EnqueueUpdate(showTimezoneList);
    await botClient.WaitForLastMessageToAsync(adminChatId);

    // Step 4: select timezone
    var timezoneSelection = UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, "timezone_Europe/Moscow");
    botClient.EnqueueUpdate(timezoneSelection);
    await botClient.WaitForLastMessageToAsync(adminChatId);

    // Now admin has a family and main menu is shown

    // Step 5: open family menu via /family
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "/family"));

    var familyMenuMessage = await botClient.WaitForLastMessageToAsync(adminChatId);
    familyMenuMessage.ShouldNotBeNull("Бот должен показать меню семьи после команды /family");
    familyMenuMessage!.ShouldContainText(familyName);
    var familyMenuKeyboard = familyMenuMessage.ShouldHaveInlineKeyboard();
    var createInviteButton = familyMenuKeyboard.GetButton("Создать приглашение");
    createInviteButton.CallbackData.ShouldNotBeNull();

    // Step 6: click "Create invite" button
    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createInviteButton.CallbackData!));

    // Step 7: select role for invite (Adult)
    var inviteRoleMessage = await botClient.WaitForLastMessageToAsync(adminChatId);
    inviteRoleMessage.ShouldNotBeNull("Бот должен показать выбор роли для приглашения");
    inviteRoleMessage!.ShouldContainText("Создание приглашения");
    var inviteRoleKeyboard = inviteRoleMessage.ShouldHaveInlineKeyboard();
    var adultRoleButton = inviteRoleKeyboard.GetButton("Взрослый");
    adultRoleButton.CallbackData.ShouldNotBeNull();

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, adultRoleButton.CallbackData!));

    // Step 8: get invite link with payload
    var inviteMessage = await botClient.WaitForLastMessageToAsync(adminChatId);
    inviteMessage.ShouldNotBeNull("Бот должен отправить сообщение о создании приглашения");
    inviteMessage!.ShouldContainText("Приглашение создано");

    var inviteText = inviteMessage.Text!;
    var match = InviteCodeRegex().Match(inviteText);
    match.Success.ShouldBeTrue("Пригласительная ссылка должна содержать payload вида invite_CODE");
    var invitePayload = match.Value;

    // Act: invited user starts bot with invite payload
    var invitedTelegramId = TestDataBuilder.GenerateTelegramId();

    botClient.Clear();

    botClient.EnqueueUpdate(
      UpdateFactory.CreateTextUpdate(invitedTelegramId, invitedTelegramId, $"/start {invitePayload}"));

    var invitedMessages = await botClient.WaitForMessagesToAsync(invitedTelegramId, 2);
    invitedMessages.ShouldNotBeEmpty();

    invitedMessages.ShouldContain(m => m.Text != null && m.Text.Contains("Добро пожаловать в семью"));
    invitedMessages.ShouldContain(m => m.Text != null && m.Text.Contains("Главное меню"));
  }

  [GeneratedRegex(@"invite_[A-Z0-9]+")]
  private static partial Regex InviteCodeRegex();
}
