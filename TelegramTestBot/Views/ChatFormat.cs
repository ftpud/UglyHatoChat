using TelegramTestBot.Models;
using UglyFwUI.Rendering;

namespace TelegramTestBot.Views;

// Pure HTML formatting shared by the chat pages. All dynamic text is escaped.
public static class ChatFormat
{
    public const int MaxListedMembers = 10;

    public static string Members(IReadOnlyList<Chatter> members)
    {
        if (members.Count == 0) return "👥 Nobody's here. Tumbleweeds roll by 🌵";
        var listed = string.Join(", ", members.Take(MaxListedMembers)
            .Select(m => HtmlText.Encode(m.Avatar + " " + m.Nickname)));
        var more = members.Count > MaxListedMembers ? $" and {members.Count - MaxListedMembers} more" : string.Empty;
        return $"👥 <b>{members.Count}</b> online: {listed}{more}";
    }

    public static string Message(ChatMessage message, ChatPreferences preferences, string? me = null)
    {
        var time = preferences.ShowTimestamps ? $"<code>{message.SentAt:HH:mm}</code> " : string.Empty;
        var text = HtmlText.Encode(message.Text);
        var author = HtmlText.Encode(message.Author);
        var avatar = HtmlText.Encode(message.Avatar);
        var name = me != null && message.Author == me ? $"<u>{author}</u>" : author;
        var line = message.Kind switch
        {
            ChatMessageKind.Action => $"<i>{avatar} {name} {text}</i>",
            ChatMessageKind.Dice => $"🎲 <b>{name}</b> {text}",
            ChatMessageKind.System => $"<i>· {avatar} {name} {text}</i>",
            ChatMessageKind.Bot => $"{avatar} <b>{name}</b>: {text}",
            _ => $"{avatar} <b>{name}</b>: {text}"
        };
        var mentioned = me != null && preferences.HighlightMentions && message.Author != me
            && message.Text.Contains("@" + me, StringComparison.OrdinalIgnoreCase);
        return time + (mentioned ? "🔔 " + line : line);
    }
}
