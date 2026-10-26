namespace TelegramTestBot.Models;

public sealed record Chatter(long UserId, string Nickname, string Avatar)
{
    public int MessagesSent { get; init; }
}
