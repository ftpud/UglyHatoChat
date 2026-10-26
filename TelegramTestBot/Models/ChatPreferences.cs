namespace TelegramTestBot.Models;

// Per-session display settings, shared by reference between the lobby, chat and settings pages.
public sealed class ChatPreferences
{
    public bool ShowTimestamps { get; set; } = true;
    public bool HighlightMentions { get; set; } = true;
    public int PageSize { get; set; } = 8;
}
