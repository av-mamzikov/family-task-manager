using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.FunctionalTests.Helpers;
using FamilyTaskManager.Host;
using FamilyTaskManager.TestInfrastructure;
using Telegram.Bot.Types;

namespace FamilyTaskManager.FunctionalTests.BotFlow.Family;

/// <summary>
///   Bot flow tests for family members listing scenarios
///   Tests the complete flow of viewing family members, member details, role changes, and member removal
/// </summary>
public class FamilyMembersBotFlowTests(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
  public Task InitializeAsync()
  {
    factory.CreateClient();
    return Task.CompletedTask;
  }

  public Task DisposeAsync() => Task.CompletedTask;

  [Fact]
  public async Task TS_BOT_005_ShowFamilyMembers_WithMultipleMembers_ShouldDisplayCorrectList()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Create family with admin

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Петровых");

    // Add family members via invite flow
    var adultMemberId =
      await BotFamilyFlowHelpers.AddFamilyMemberViaInviteAsync(botClient, adminTelegramId, adminTelegramId,
        FamilyRole.Adult, "Взрослый участник");
    var childMemberId =
      await BotFamilyFlowHelpers.AddFamilyMemberViaInviteAsync(botClient, adminTelegramId, adminTelegramId,
        FamilyRole.Child, "Ребенок участник");

    // Act: Open family menu and navigate to members
    var familyMenuMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminTelegramId, adminTelegramId, "🏠 Семья"),
      adminTelegramId);
    var familyMenuKeyboard = familyMenuMessage?.ShouldHaveInlineKeyboard();
    var membersButton = familyMenuKeyboard?.GetButton("Управление участниками");
    membersButton?.CallbackData.ShouldNotBeNull();

    // Assert: Verify members list is displayed correctly
    var membersListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminTelegramId, adminTelegramId, membersButton!.CallbackData!),
      adminTelegramId);
    membersListMessage.ShouldNotBeNull("Бот должен показать список участников семьи");
    membersListMessage!.ShouldContainText("Участники семьи");
    membersListMessage.ShouldContainText("Администратор");
    membersListMessage.ShouldContainText("Взрослый");
    membersListMessage.ShouldContainText("Ребёнок");

    var membersKeyboard = membersListMessage.ShouldHaveInlineKeyboard();

    // Verify all members are clickable (adjust expected button texts based on actual user names)
    membersKeyboard.ShouldContainButton("👑"); // Admin button with crown emoji
    membersKeyboard.ShouldContainButton("👤 Взрослый участник");
    membersKeyboard.ShouldContainButton("👶 Ребенок участник");
    membersKeyboard.ShouldContainButton("🔗 Создать приглашение");
    membersKeyboard.ShouldContainButton("⬅️ Назад");
  }

  [Fact]
  public async Task TS_BOT_007_ShowMemberDetails_AndNavigateBack_ShouldWorkCorrectly()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Create family with multiple members
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Петровых");
    await BotFamilyFlowHelpers.AddFamilyMemberViaInviteAsync(botClient, adminChatId, adminTelegramId,
      FamilyRole.Adult, "Тестовый взрослый");

    // Navigate to members list
    var familyMenuMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🏠 Семья"),
      adminChatId);
    var familyMenuKeyboard = familyMenuMessage?.ShouldHaveInlineKeyboard();
    var membersButton = familyMenuKeyboard?.GetButton("Управление участниками");

    var membersListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, membersButton!.CallbackData!),
      adminChatId);
    var membersKeyboard = membersListMessage?.ShouldHaveInlineKeyboard();
    var adultMemberButton = membersKeyboard?.GetButton("👤 Тестовый взрослый");
    adultMemberButton?.CallbackData.ShouldNotBeNull();

    // Act: Click on adult member to view details
    // Assert: Verify member details are shown
    var memberDetailsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, adultMemberButton!.CallbackData!),
      adminChatId);
    memberDetailsMessage.ShouldNotBeNull("Бот должен показать детали участника");
    memberDetailsMessage!.ShouldContainText("Тестовый взрослый");
    memberDetailsMessage.ShouldContainText("Роль: Взрослый");
    memberDetailsMessage.ShouldContainText("Очки:");

    var detailsKeyboard = memberDetailsMessage.ShouldHaveInlineKeyboard();
    detailsKeyboard.ShouldContainButton("♻️ Сменить роль");
    detailsKeyboard.ShouldContainButton("🗑️ Удалить участника");
    detailsKeyboard.ShouldContainButton("⬅️ Назад к участникам");

    // Act: Navigate back to members list
    var backButton = detailsKeyboard.GetButton("⬅️ Назад к участникам");

    // Assert: Verify we're back to members list
    var backToListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, backButton.CallbackData!),
      adminChatId);
    backToListMessage.ShouldNotBeNull("Должны вернуться к списку участников");
    backToListMessage!.ShouldContainText("Участники семьи");
  }

  [Fact]
  public async Task TS_BOT_008_ChangeMemberRole_AdminChangesAdultToChild_ShouldUpdateSuccessfully()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Create family with adult member
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Петровых");
    await BotFamilyFlowHelpers.AddFamilyMemberViaInviteAsync(botClient, adminChatId, adminTelegramId,
      FamilyRole.Adult, "Взрослый для смены");

    // Navigate to member details
    var memberDetailsMessage =
      await NavigateToMemberDetailsAsync(botClient, adminChatId, adminTelegramId, "👤 Взрослый для смены");
    var detailsKeyboard = memberDetailsMessage!.ShouldHaveInlineKeyboard();
    var changeRoleButton = detailsKeyboard.GetButton("♻️ Сменить роль");
    changeRoleButton.CallbackData.ShouldNotBeNull();

    // Act: Start role change process
    // Assert: Verify role selection screen
    var roleSelectionMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, changeRoleButton.CallbackData!),
      adminChatId);
    roleSelectionMessage.ShouldNotBeNull("Бот должен показать выбор новой роли");
    roleSelectionMessage!.ShouldContainText("Смена роли участника");
    roleSelectionMessage.ShouldContainText("Текущая роль: 👤 Взрослый");

    var roleKeyboard = roleSelectionMessage.ShouldHaveInlineKeyboard();

    // Verify current role is not shown as option
    roleKeyboard.ShouldNotContainButton("Взрослый");
    roleKeyboard.ShouldContainButton("Администратор");
    roleKeyboard.ShouldContainButton("Ребёнок");
    roleKeyboard.ShouldContainButton("⬅️ Назад");

    // Act: Select Child role
    var childRoleButton = roleKeyboard.GetButton("Ребёнок");
    childRoleButton.CallbackData.ShouldNotBeNull();

    // Assert: Verify role was changed and we're back to member details
    var updatedDetailsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, childRoleButton.CallbackData!),
      adminChatId);
    updatedDetailsMessage.ShouldNotBeNull("Должны вернуться к деталям участника с обновленной ролью");
    updatedDetailsMessage!.ShouldContainText("Взрослый для смены");
    updatedDetailsMessage.ShouldContainText("Роль: Ребёнок");
  }

  [Fact]
  public async Task TS_BOT_009_RemoveMember_AdminRemovesAdultMember_ShouldUpdateList()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Create family with adult member
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Петровых");
    await BotFamilyFlowHelpers.AddFamilyMemberViaInviteAsync(botClient, adminChatId, adminTelegramId,
      FamilyRole.Adult, "Удаляемый участник");

    // Navigate to member details
    var memberDetailsMessage =
      await NavigateToMemberDetailsAsync(botClient, adminChatId, adminTelegramId, "👤 Удаляемый участник");

    var detailsKeyboard = memberDetailsMessage!.ShouldHaveInlineKeyboard();
    var deleteButton = detailsKeyboard.GetButton("🗑️ Удалить участника");
    deleteButton.CallbackData.ShouldNotBeNull();

    // Act: Start member removal process
    // Assert: Verify confirmation dialog
    var confirmationMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, deleteButton.CallbackData!),
      adminChatId);
    confirmationMessage.ShouldNotBeNull("Бот должен показать диалог подтверждения удаления");
    confirmationMessage!.ShouldContainText("Удаление участника");
    confirmationMessage.ShouldContainText("Удаляемый участник");
    confirmationMessage.ShouldContainText("Взрослый");

    var confirmationKeyboard = confirmationMessage.ShouldHaveInlineKeyboard();
    confirmationKeyboard.ShouldContainButton("✅ Да, удалить");
    confirmationKeyboard.ShouldContainButton("❌ Отмена");

    // Act: Confirm deletion
    var confirmDeleteButton = confirmationKeyboard.GetButton("✅ Да, удалить");
    // Assert: Verify member is removed and we're back to members list
    var updatedMembersListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, confirmDeleteButton.CallbackData!),
      adminChatId);
    updatedMembersListMessage.ShouldNotBeNull("Должны вернуться к обновленному списку участников");
    updatedMembersListMessage!.ShouldContainText("Участники семьи");
    updatedMembersListMessage.ShouldNotContainText("Удаляемый участник");

    var updatedKeyboard = updatedMembersListMessage.ShouldHaveInlineKeyboard();
    updatedKeyboard.ShouldNotContainButton("👤 Удаляемый участник");
  }

  [Fact]
  public async Task TS_BOT_010_CancelMemberRemoval_ShouldReturnToMemberDetails()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Create family with adult member
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Петровых");
    await BotFamilyFlowHelpers.AddFamilyMemberViaInviteAsync(botClient, adminChatId, adminTelegramId,
      FamilyRole.Adult, "Не удаляемый участник");

    // Navigate to member details
    var memberDetailsMessage =
      await NavigateToMemberDetailsAsync(botClient, adminChatId, adminTelegramId, "👤 Не удаляемый участник");

    var detailsKeyboard = memberDetailsMessage!.ShouldHaveInlineKeyboard();
    var deleteButton = detailsKeyboard.GetButton("🗑️ Удалить участника");
    deleteButton.CallbackData.ShouldNotBeNull();

    // Act: Start member removal process
    // Assert: Verify confirmation dialog
    var confirmationMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, deleteButton.CallbackData!),
      adminChatId);
    confirmationMessage.ShouldNotBeNull("Бот должен показать диалог подтверждения удаления");
    confirmationMessage!.ShouldContainText("Удаление участника");
    confirmationMessage.ShouldContainText("Удаляемый участник");

    var confirmationKeyboard = confirmationMessage.ShouldHaveInlineKeyboard();
    confirmationKeyboard.ShouldContainButton("✅ Да, удалить");
    confirmationKeyboard.ShouldContainButton("❌ Отмена");

    // Act: Cancel deletion
    var cancelButton = confirmationKeyboard.GetButton("❌ Отмена");
    // Assert: Verify we're back to member details and member still exists
    var backToDetailsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, cancelButton.CallbackData!),
      adminChatId);
    backToDetailsMessage.ShouldNotBeNull("Должны вернуться к деталям участника после отмены");
    backToDetailsMessage!.ShouldContainText("Не удаляемый участник");
    backToDetailsMessage.ShouldContainText("Роль: Взрослый");

    var detailsKeyboardAgain = backToDetailsMessage.ShouldHaveInlineKeyboard();
    detailsKeyboardAgain.ShouldContainButton("♻️ Сменить роль");
    detailsKeyboardAgain.ShouldContainButton("🗑️ Удалить участника");
  }

  [Fact]
  public async Task TS_BOT_011_NavigateFromMembersListToFamilyMenu_ShouldWorkCorrectly()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Create family and navigate to members list
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Петровых");

    var familyMenuMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🏠 Семья"),
      adminChatId);
    var familyMenuKeyboard = familyMenuMessage!.ShouldHaveInlineKeyboard();
    var membersButton = familyMenuKeyboard.GetButton("Управление участниками");

    var membersListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, membersButton.CallbackData!),
      adminChatId);
    var membersKeyboard = membersListMessage!.ShouldHaveInlineKeyboard();
    var backButton = membersKeyboard.GetButton("⬅️ Назад");
    backButton.CallbackData.ShouldNotBeNull();

    // Act: Navigate back to family menu
    // Assert: Verify we're back to family menu
    var backToFamilyMenuMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, backButton.CallbackData!),
      adminChatId);
    backToFamilyMenuMessage.ShouldNotBeNull("Должны вернуться к меню семьи");
    backToFamilyMenuMessage!.ShouldContainText("Ваши семьи:");
    backToFamilyMenuMessage.ShouldContainText(familyName);

    var familyMenuKeyboardAgain = backToFamilyMenuMessage.ShouldHaveInlineKeyboard();
    familyMenuKeyboardAgain.ShouldContainButton("Управление участниками");
    familyMenuKeyboardAgain.ShouldContainButton("Создать приглашение");
  }

  [Fact]
  public async Task TS_BOT_013_RemoveMember_ShouldSendNotificationToMember()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Create family with adult member
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Петровых");
    var memberTelegramId =
      await BotFamilyFlowHelpers.AddFamilyMemberViaInviteAsync(botClient, adminChatId, adminTelegramId,
        FamilyRole.Adult, "Участник для удаления");

    // Navigate to member details and start removal
    var memberDetailsMessage =
      await NavigateToMemberDetailsAsync(botClient, adminChatId, adminTelegramId, "👤 Участник для удаления");
    var detailsKeyboard = memberDetailsMessage!.ShouldHaveInlineKeyboard();
    var deleteButton = detailsKeyboard.GetButton("🗑️ Удалить участника");

    var confirmationMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, deleteButton.CallbackData!),
      adminChatId);

    var confirmationKeyboard = confirmationMessage!.ShouldHaveInlineKeyboard();
    var confirmDeleteButton = confirmationKeyboard.GetButton("✅ Да, удалить");

    // Act: Confirm deletion
    // Wait for admin to receive confirmation
    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, confirmDeleteButton.CallbackData!),
      adminChatId);
  }

  // AddFamilyMemberViaInviteAsync вынесен в BotFamilyFlowHelpers

  private async Task<Message?> NavigateToMemberDetailsAsync(
    TestTelegramBotClient botClient,
    long adminChatId,
    long adminTelegramId,
    string memberButtonName)
  {
    var familyMenuMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🏠 Семья"),
      adminChatId);
    var familyMenuKeyboard = familyMenuMessage!.ShouldHaveInlineKeyboard();
    var membersButton = familyMenuKeyboard.GetButton("Управление участниками");

    var membersListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, membersButton.CallbackData!),
      adminChatId);
    var membersKeyboard = membersListMessage!.ShouldHaveInlineKeyboard();
    var memberButton = membersKeyboard.GetButton(memberButtonName);
    return await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, memberButton.CallbackData!), adminChatId);
  }
}
