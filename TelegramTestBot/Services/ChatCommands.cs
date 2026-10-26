using TelegramTestBot.Models;

namespace TelegramTestBot.Services;

public sealed record ChatPost(ChatMessageKind Kind, string Text);

// Turns typed text into a chat post. Unknown commands return null.
public static class ChatCommands
{
    public const int MaxLength = 300;

    public const string Help =
        "/me dances — act it out\n/roll [sides] — roll a die\n/flip — flip a coin\n" +
        "/8ball question — ask the oracle\n/shrug [text] — ¯\\_(ツ)_/¯\n/hug nick — spread love\n" +
        "Mention someone with @nick. Say \"hato\" and the pigeon answers.";

    private static readonly string[] EightBall =
    {
        "It is certain.", "Ask again after coffee.", "My sources say no.", "Signs point to yes.",
        "Absolutely not. Never. Okay maybe.", "The pigeon says yes 🐦", "Outlook hazy, try /roll instead."
    };

    public static string? Validate(string text) => string.IsNullOrWhiteSpace(text)
        ? "Say something! Silence is golden, but not here."
        : text.Length > MaxLength ? $"Keep it under {MaxLength} characters, poet." : null;

    public static bool IsHelp(string text) => text.Trim().Equals("/help", StringComparison.OrdinalIgnoreCase);

    // Also rejects unknown commands, so a message dialog can show the error in place.
    public static string? ValidateMessage(string text) => Validate(text)
        ?? (IsHelp(text) || Interpret(text, Random.Shared) != null ? null : "Unknown command 🤔 Try /help.");

    public static ChatPost? Interpret(string text, Random random)
    {
        text = text.Trim();
        if (!text.StartsWith('/')) return new ChatPost(ChatMessageKind.Say, text);
        var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var argument = parts.Length == 2 ? parts[1].Trim() : string.Empty;
        switch (parts[0].Split('@', 2)[0].ToLowerInvariant())
        {
            case "/me" when argument.Length > 0:
                return new ChatPost(ChatMessageKind.Action, argument);
            case "/roll":
                var sides = int.TryParse(argument, out var requested) && requested is >= 2 and <= 1000 ? requested : 6;
                var roll = random.Next(1, sides + 1);
                var flavour = roll == sides ? " 🎉 CRIT!" : roll == 1 ? " 💀 ouch" : string.Empty;
                return new ChatPost(ChatMessageKind.Dice, $"rolled {roll} on a d{sides}{flavour}");
            case "/flip":
                return new ChatPost(ChatMessageKind.Dice, random.Next(2) == 0
                    ? "flipped a coin: 🪙 Heads" : "flipped a coin: 🪙 Tails");
            case "/8ball" when argument.Length > 0:
                return new ChatPost(ChatMessageKind.Dice, $"asked the 🎱 \"{argument}\" — {EightBall[random.Next(EightBall.Length)]}");
            case "/shrug":
                return new ChatPost(ChatMessageKind.Say, (argument + " ¯\\_(ツ)_/¯").TrimStart());
            case "/hug" when argument.Length > 0:
                return new ChatPost(ChatMessageKind.Action, $"hugs {argument} 🤗");
            default:
                return null;
        }
    }
}
