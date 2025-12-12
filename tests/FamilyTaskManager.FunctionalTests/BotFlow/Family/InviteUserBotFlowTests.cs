using System.Text.RegularExpressions;
using FamilyTaskManager.FunctionalTests.Helpers;
using FamilyTaskManager.Host;

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

  [RetryFact(3)]
  public async Task TS_BOT_002_InviteUserViaStartCommand_ShouldJoinFamilyAndShowMainMenu()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Admin creates family via bot flow
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Петровых");

    // Step 5: open family menu via main menu button
    var familyMenuMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🏠 Семья"),
      adminTelegramId);
    familyMenuMessage.ShouldNotBeNull("Бот должен показать меню семьи после нажатия на кнопку '🏠 Семья'");
    familyMenuMessage!.ShouldContainText(familyName);
    var familyMenuKeyboard = familyMenuMessage.ShouldHaveInlineKeyboard();
    var createInviteButton = familyMenuKeyboard.GetButton("Создать приглашение");
    createInviteButton.CallbackData.ShouldNotBeNull();

    // Step 6: click "Create invite" button
    // Step 7: select role for invite (Adult)
    var inviteRoleMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createInviteButton.CallbackData!),
      adminChatId);
    inviteRoleMessage.ShouldNotBeNull("Бот должен показать выбор роли для приглашения");
    inviteRoleMessage!.ShouldContainText("Создание приглашения");
    var inviteRoleKeyboard = inviteRoleMessage.ShouldHaveInlineKeyboard();
    var adultRoleButton = inviteRoleKeyboard.GetButton("Взрослый");
    adultRoleButton.CallbackData.ShouldNotBeNull();

    var inviteMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, adultRoleButton.CallbackData!),
      adminTelegramId);
    inviteMessage.ShouldNotBeNull("Бот должен отправить сообщение о создании приглашения");
    inviteMessage!.ShouldContainText("Приглашение создано");

    var inviteText = inviteMessage.Text!;
    var match = InviteCodeRegex().Match(inviteText);
    match.Success.ShouldBeTrue("Пригласительная ссылка должна содержать payload вида invite_CODE");
    var invitePayload = match.Value;

    // Act: invited user starts bot with invite payload
    var invitedTelegramId = TestDataBuilder.GenerateTelegramId();

    botClient.Clear();

    var invitedMessages = await botClient.SendUpdateAndWaitForMessagesAsync(
      UpdateFactory.CreateTextUpdate(invitedTelegramId, invitedTelegramId, $"/start {invitePayload}"),
      invitedTelegramId,
      1);
    invitedMessages.ShouldNotBeEmpty();

    invitedMessages.ShouldContain(m => m.Text != null && m.Text.Contains("Добро пожаловать в семью"));
    invitedMessages.ShouldContain(m => m.Text != null && m.Text.Contains("Главное меню"));
  }

  [GeneratedRegex(@"invite_[A-Z0-9]+")]
  private static partial Regex InviteCodeRegex();
}
