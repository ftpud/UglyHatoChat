namespace TelegramTestBot.Models;

public enum ChatMessageKind
{
    Say,
    Action,
    Dice,
    System,
    Bot
}

public sealed record ChatMessage(long Id, DateTimeOffset SentAt, ChatMessageKind Kind, string Author, string Avatar,
    string Text);
