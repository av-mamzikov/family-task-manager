using System.Text.RegularExpressions;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Host;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.TestInfrastructure;

namespace FamilyTaskManager.FunctionalTests.Helpers;

public static class BotFamilyFlowHelpers
{
  public static async Task<(string FamilyName, long AdminTelegramId, long AdminChatId)>
    CreateFamilyByGeolocationAsync(
      CustomWebApplicationFactory<Program> factory,
      string? familyName = null,
      double latitude = 55.7558,
      double longitude = 37.6173)
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var adminTelegramId = TestDataBuilder.GenerateTelegramId();
    var adminChatId = adminTelegramId;
    var actualFamilyName = familyName ?? "Test Family";

    await botClient.SendUpdateAndWaitForMessagesAsync([
        UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, CallbackData.Family.Create()),
        UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, actualFamilyName),
        UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, CallbackData.FamilyCreation.DetectTimezone()),
        UpdateFactory.CreateLocationUpdate(adminChatId, adminTelegramId, latitude, longitude)
      ], adminChatId,
      m => m.Text!.Contains("Главное меню")
    );

    return (actualFamilyName, adminTelegramId, adminChatId);
  }

  public static async Task<long> AddFamilyMemberViaInviteAsync(
    TestTelegramBotClient botClient,
    long adminChatId,
    long adminTelegramId,
    FamilyRole role,
    string memberName)
  {
    // Create invite
    var familyMenuMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🏠 Семья"),
      adminChatId);
    var familyMenuKeyboard = familyMenuMessage!.ShouldHaveInlineKeyboard();
    var createInviteButton = familyMenuKeyboard.GetButton("Создать приглашение");

    var inviteRoleMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createInviteButton.CallbackData!),
      adminChatId);
    var inviteRoleKeyboard = inviteRoleMessage!.ShouldHaveInlineKeyboard();
    var roleButton = inviteRoleKeyboard.GetButton(RoleDisplay.GetRoleCaption(role));

    var inviteMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, roleButton.CallbackData!),
      adminChatId);
    var inviteText = inviteMessage!.Text!;
    var match = Regex.Match(inviteText, @"invite_[A-Z0-9]+");
    var invitePayload = match.Value;

    // Join family with new user
    var newMemberTelegramId = TestDataBuilder.GenerateTelegramId();
    botClient.Clear();

    await botClient.SendUpdateAndWaitForMessagesAsync(
      UpdateFactory.CreateTextUpdate(newMemberTelegramId, newMemberTelegramId,
        $"/start {invitePayload}", firstName: memberName),
      newMemberTelegramId,
      2);

    return newMemberTelegramId;
  }
}
