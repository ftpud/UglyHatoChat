using TelegramTestBot.Models;
using UglyAppFramework.DependencyManager.Attributes;
using UglyTgApplication;
using UglyTgApplication.States;

namespace TelegramTestBot.Services;

// One shared in-memory room. Every change asks watching pages of other users to re-render.
[Managed]
public class ChatRoom
{
    public const int HistoryLimit = 500;
    public const string BotName = "Hato";

    private static readonly string[] JoinLines =
    {
        "slid into the chat 😎", "appeared in a puff of glitter ✨", "joined. Everyone act natural 👀",
        "has entered the building 🏢", "spawned 🎮"
    };
    private static readonly string[] LeaveLines =
    {
        "left to touch grass 🌱", "vanished in a puff of smoke 💨", "rage-quit 🙃", "went to get snacks 🍕"
    };
    private static readonly string[] PigeonLines = { "coo coo! 🐦", "COO. 🐦", "*pecks at your keyboard* 🐦", "brrrr coo 🐦" };

    private readonly object _sync = new();
    private readonly List<ChatMessage> _messages = new();
    private readonly Dictionary<long, Chatter> _online = new();
    private readonly Dictionary<string, int> _messageCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ViewModel> _watchers = new();
    private long _nextId;

    [Inject] private ViewRefreshQueue Refresh { get; set; } = null!;
    public Random Random { get; set; } = new();
    public Func<DateTimeOffset> Clock { get; set; } = () => DateTimeOffset.UtcNow;

    public IReadOnlyList<ChatMessage> Messages
    {
        get { lock (_sync) return _messages.ToArray(); }
    }

    public IReadOnlyList<Chatter> Online
    {
        get
        {
            lock (_sync)
                return _online.Values.Select(m => m with { MessagesSent = _messageCounts.GetValueOrDefault(m.Nickname) })
                    .OrderBy(m => m.Nickname, StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }

    public IReadOnlyList<(string Nickname, int Count)> Leaderboard
    {
        get
        {
            lock (_sync)
                return _messageCounts.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
                    .Take(5).Select(p => (p.Key, p.Value)).ToArray();
        }
    }

    // Random is not thread-safe, so dice share the room lock.
    public ChatPost? Interpret(string text)
    {
        lock (_sync) return ChatCommands.Interpret(text, Random);
    }

    public void Watch(ViewModel page)
    {
        lock (_sync)
            if (!_watchers.Contains(page)) _watchers.Add(page);
    }

    public void Unwatch(ViewModel page)
    {
        lock (_sync) _watchers.Remove(page);
    }

    public void Join(Chatter member, ViewModel? origin = null)
    {
        lock (_sync)
        {
            if (_online.Count == 0 && _messages.Count == 0)
                Append(ChatMessageKind.Bot, BotName, "🐦", "Welcome to the very first chat of this restart! Type /help for tricks.");
            _online[member.UserId] = member;
            Append(ChatMessageKind.System, member.Nickname, member.Avatar, Pick(JoinLines));
        }
        NotifyExcept(origin);
    }

    public void Leave(long userId, ViewModel? origin = null)
    {
        lock (_sync)
        {
            if (!_online.Remove(userId, out var member)) return;
            Append(ChatMessageKind.System, member.Nickname, member.Avatar, Pick(LeaveLines));
        }
        NotifyExcept(origin);
    }

    public ChatMessage Post(Chatter author, ChatPost post, ViewModel? origin = null)
    {
        ChatMessage message;
        lock (_sync)
        {
            message = Append(post.Kind, author.Nickname, author.Avatar, post.Text);
            _messageCounts[author.Nickname] = _messageCounts.GetValueOrDefault(author.Nickname) + 1;
            if (post.Kind == ChatMessageKind.Say && post.Text.Contains("hato", StringComparison.OrdinalIgnoreCase))
                Append(ChatMessageKind.Bot, BotName, "🐦", Pick(PigeonLines));
        }
        NotifyExcept(origin);
        return message;
    }

    private ChatMessage Append(ChatMessageKind kind, string author, string avatar, string text)
    {
        var message = new ChatMessage(++_nextId, Clock(), kind, author, avatar, text);
        _messages.Add(message);
        if (_messages.Count > HistoryLimit) _messages.RemoveAt(0);
        return message;
    }

    private string Pick(string[] lines) => lines[Random.Next(lines.Length)];

    // The origin renders itself in its own awaited handler; others are queued, never awaited here.
    private void NotifyExcept(ViewModel? origin)
    {
        ViewModel[] watchers;
        lock (_sync) watchers = _watchers.Where(w => !ReferenceEquals(w, origin)).ToArray();
        foreach (var watcher in watchers) Refresh.Request(watcher);
    }
}
