using UglyAppFramework.DependencyManager.Attributes;

namespace TelegramTestBot.Services;

// In-memory nickname ownership: names are unique (case-insensitively) until restart.
[Managed]
public class NicknameRegistry
{
    public const int MinLength = 2;
    public const int MaxLength = 16;
    private static readonly string[] Reserved = { "hato", "system", "admin", "bot" };
    private readonly object _sync = new();
    private readonly Dictionary<long, string> _byUser = new();
    private readonly Dictionary<string, long> _byName = new(StringComparer.OrdinalIgnoreCase);

    public string? Get(long userId)
    {
        lock (_sync) return _byUser.TryGetValue(userId, out var name) ? name : null;
    }

    public static string? Validate(string nickname)
    {
        if (nickname.Length < MinLength || nickname.Length > MaxLength)
            return $"Nicknames are {MinLength}–{MaxLength} characters long.";
        if (!nickname.All(c => char.IsLetterOrDigit(c) || c is '_' or '-'))
            return "Letters, digits, '_' and '-' only. No spaces, no 💩.";
        if (Reserved.Contains(nickname, StringComparer.OrdinalIgnoreCase))
            return "Nice try 😏 That name is reserved.";
        return null;
    }

    // Atomically checks and claims, releasing the user's previous name.
    public string? TryClaim(long userId, string nickname)
    {
        if (Validate(nickname) is { } error) return error;
        lock (_sync)
        {
            if (_byName.TryGetValue(nickname, out var owner) && owner != userId)
                return $"\"{nickname}\" is already taken. Be more original 🙃";
            if (_byUser.TryGetValue(userId, out var previous)) _byName.Remove(previous);
            _byUser[userId] = nickname;
            _byName[nickname] = userId;
            return null;
        }
    }
}
