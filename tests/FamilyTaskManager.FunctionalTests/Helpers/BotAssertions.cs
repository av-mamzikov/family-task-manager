using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.FunctionalTests.Helpers;

/// <summary>
///   Extension methods for asserting bot responses in tests
/// </summary>
public static class BotAssertions
{
  /// <summary>
  ///   Assert that message contains expected text
  /// </summary>
  public static void ShouldContainText(this Message message, string expectedText)
  {
    message.ShouldNotBeNull("Message should not be null");
    message.Text.ShouldNotBeNull("Message text should not be null");
    message.Text.ShouldContain(expectedText);
  }

  /// <summary>
  ///   Assert that message has inline keyboard
  /// </summary>
  public static InlineKeyboardMarkup ShouldHaveInlineKeyboard(this Message message)
  {
    message.ShouldNotBeNull("Message should not be null");
    message.ReplyMarkup.ShouldNotBeNull("Message should have reply markup");
    return message.ReplyMarkup.ShouldBeOfType<InlineKeyboardMarkup>();
  }

  /// <summary>
  ///   Assert that message has reply keyboard
  /// </summary>
  public static ReplyKeyboardMarkup ShouldHaveReplyKeyboard(this Message message)
  {
    message.ShouldNotBeNull("Message should not be null");
    message.ReplyMarkup.ShouldNotBeNull("Message should have reply markup");
    return message.ReplyMarkup.ShouldBeOfType<ReplyKeyboardMarkup>();
  }

  /// <summary>
  ///   Assert that inline keyboard contains button with specific text
  /// </summary>
  public static void ShouldContainButton(this InlineKeyboardMarkup keyboard, string buttonText)
  {
    keyboard.ShouldNotBeNull("Keyboard should not be null");
    var hasButton = keyboard.InlineKeyboard
      .Any(row => row.Any(btn => btn.Text.Contains(buttonText)));
    hasButton.ShouldBeTrue($"Keyboard should contain button with text: {buttonText}");
  }

  /// <summary>
  ///   Assert that inline keyboard contains button with specific callback data
  /// </summary>
  public static void ShouldContainButtonWithCallback(this InlineKeyboardMarkup keyboard, string callbackData)
  {
    keyboard.ShouldNotBeNull("Keyboard should not be null");
    var hasButton = keyboard.InlineKeyboard
      .Any(row => row.Any(btn => btn.CallbackData == callbackData));
    hasButton.ShouldBeTrue($"Keyboard should contain button with callback data: {callbackData}");
  }

  /// <summary>
  ///   Assert that reply keyboard contains button with specific text
  /// </summary>
  public static void ShouldContainButton(this ReplyKeyboardMarkup keyboard, string buttonText)
  {
    keyboard.ShouldNotBeNull("Keyboard should not be null");
    var hasButton = keyboard.Keyboard
      .Any(row => row.Any(btn => btn.Text.Contains(buttonText)));
    hasButton.ShouldBeTrue($"Keyboard should contain button with text: {buttonText}");
  }

  /// <summary>
  ///   Get button by text from inline keyboard
  /// </summary>
  public static InlineKeyboardButton GetButton(this InlineKeyboardMarkup keyboard, string buttonText)
  {
    keyboard.ShouldNotBeNull("Keyboard should not be null");
    var button = keyboard.InlineKeyboard
      .SelectMany(row => row)
      .FirstOrDefault(btn => btn.Text.Contains(buttonText));
    button.ShouldNotBeNull($"Button with text '{buttonText}' not found");
    return button;
  }

  /// <summary>
  ///   Assert that message was sent to specific chat
  /// </summary>
  public static void ShouldBeSentTo(this Message message, long chatId)
  {
    message.ShouldNotBeNull("Message should not be null");
    message.Chat.ShouldNotBeNull("Message chat should not be null");
    message.Chat.Id.ShouldBe(chatId);
  }
}
