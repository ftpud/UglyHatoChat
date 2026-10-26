using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramTestBot.Hosting;
using TelegramTestBot.Models;
using TelegramTestBot.Services;
using TelegramTestBot.ViewModels;
using TelegramTestBot.Views;
using UglyAppFramework.DependencyManager;
using UglyAppFramework.DependencyManager.Attributes;
using UglyFwUI.ViewModels;
using UglyFwUI.Views;
using UglyTgApplication;
using UglyTgApplication.States;

namespace TelegramTestBot.Tests;

public class ChatBotTests
{
    private const long Alice = 1, Bob = 2;

    [Test]
    public async Task NicknamesAreValidatedAndUniqueAcrossUsers()
    {
        using var bot = new Harness();
        await bot.Text(Alice, "/start");
        var lobby = bot.Page<LobbyViewModel>(Alice);
        await bot.Click(Alice, lobby.Enter.CallbackData);
        Assert.That(lobby.Status, Does.Contain("nickname first"));

        await bot.Register(Alice, "Alice");
        Assert.That(lobby.Nickname.Text, Is.EqualTo("Alice"));

        await bot.Text(Bob, "/start");
        await bot.Click(Bob, bot.Page<LobbyViewModel>(Bob).Nickname.CallbackData);
        foreach (var (attempt, error) in new[] { ("alice", "already taken"), ("Hato", "reserved"), ("a b", "Letters"), ("x", "2–16") })
        {
            await bot.Text(Bob, attempt);
            Assert.That(bot.Page<TextDialogViewModel>(Bob).ErrorMessage, Does.Contain(error), attempt);
        }
        await bot.Text(Bob, "Bob");
        Assert.That(bot.Page<LobbyViewModel>(Bob).Nickname.Text, Is.EqualTo("Bob"));

        // Renaming frees the old name for others.
        await bot.Register(Alice, "Alicia");
        Assert.That(bot.Registry.TryClaim(Bob, "Alice"), Is.Null);
    }

    [Test]
    public async Task MessagesReachOtherUsersReactivelyAndInputIsRemoved()
    {
        using var bot = new Harness();
        await bot.Enter(Alice, "Alice");
        await bot.Enter(Bob, "Bob");
        await bot.Refresh.ProcessPendingAsync();
        var bobView = bot.Transport.LastText(Bob);
        Assert.That(bobView, Does.Contain("Bob").And.Contain("2</b> online"));

        bot.Transport.Calls.Clear();
        await bot.Text(Alice, "hi <@Bob> & friends");
        Assert.That(bot.Transport.Calls.Any(c => c.Method == "deleteMessage" && c.ChatId == Alice), Is.True);
        await bot.Refresh.ProcessPendingAsync();

        var update = bot.Transport.Calls.Last(c => c.ChatId == Bob);
        Assert.That(update.Method, Is.EqualTo("editMessageText"));
        Assert.That(update.Text, Does.Contain("hi &lt;@Bob&gt; &amp; friends").And.Contain("🔔"));
        Assert.That(bot.Transport.LastText(Alice), Does.Contain("<u>Alice</u>").And.Not.Contain("🔔"));
    }

    [Test]
    public async Task MessageButtonPostsInOneStepAndRejectsBadInputInPlace()
    {
        using var bot = new Harness();
        var chat = await bot.Enter(Alice, "Alice");
        await bot.Click(Alice, chat.Compose.CallbackData);
        var dialog = bot.Page<TextDialogViewModel>(Alice);
        await bot.Text(Alice, new string('x', ChatCommands.MaxLength + 1));
        Assert.That(dialog.ErrorMessage, Does.Contain("under"));
        await bot.Text(Alice, "/teleport");
        Assert.That(dialog.ErrorMessage, Does.Contain("Unknown command"));
        Assert.That(bot.Current(Alice), Is.SameAs(dialog));

        await bot.Text(Alice, "/roll 20");
        Assert.That(bot.Current(Alice), Is.SameAs(chat));
        Assert.That(bot.Room.Messages.Last().Text, Does.Match(@"^rolled \d+ on a d20"));
        Assert.That(bot.Transport.LastText(Alice), Does.Contain("on a d20"));

        await bot.Click(Alice, chat.Compose.CallbackData);
        await bot.Text(Alice, "/help");
        Assert.That(bot.Current(Alice), Is.SameAs(chat));
        Assert.That(chat.Status, Does.Contain("/8ball"));

        await bot.Click(Alice, chat.QuickActions[2].CallbackData);
        Assert.That(bot.Room.Messages.Last(), Has.Property("Kind").EqualTo(ChatMessageKind.Action)
            .And.Property("Text").EqualTo("waves 👋"));

        await bot.Text(Alice, "/nope");
        Assert.That(chat.Status, Does.Contain("Unknown command"));
    }

    [Test]
    public async Task ManualScrollStaysPutWhileOthersTalkAndLatestResumesAutoScroll()
    {
        using var bot = new Harness();
        var bobChat = await bot.Enter(Bob, "Bob");
        await bot.Enter(Alice, "Alice");
        for (var i = 0; i < 20; i++) await bot.Text(Alice, "msg " + i);
        Assert.That(bobChat.History.GetWindow().Items.Last().Text, Is.EqualTo("msg 19"));

        await bot.Click(Bob, bobChat.History.OlderCallbackData);
        var frozen = bobChat.History.GetWindow();
        Assert.That(frozen.AutoScroll, Is.False);
        await bot.Text(Alice, "you can't see me");
        var still = bobChat.History.GetWindow();
        Assert.That(still.Items.Select(m => m.Id), Is.EqualTo(frozen.Items.Select(m => m.Id)));
        Assert.That(still.Newer, Is.EqualTo(frozen.Newer + 1));
        await bot.Refresh.ProcessPendingAsync();
        Assert.That(bot.Transport.LastText(Bob), Does.Contain($"▼ {still.Newer} newer"));
        Assert.That(ButtonLabels(bobChat), Does.Contain("✋ Manual"));

        await bot.Click(Bob, bobChat.History.LatestCallbackData);
        Assert.That(bobChat.History.GetWindow().Items.Last().Text, Is.EqualTo("you can't see me"));
        Assert.That(bot.Transport.LastText(Bob), Does.Not.Contain("newer"));
        Assert.That(ButtonLabels(bobChat), Does.Contain("📌 Auto"));
    }

    private static IEnumerable<string> ButtonLabels(ChatRoomViewModel chat) =>
        new ChatRoomView().Display(chat).ResponseMessages.Single().replyMarkup!.InlineKeyboard
            .SelectMany(row => row).Select(button => button.Text);

    [Test]
    public async Task SettingsChangeTimestampsAndPageSize()
    {
        using var bot = new Harness();
        var chat = await bot.Enter(Alice, "Alice");
        for (var i = 0; i < 10; i++) await bot.Text(Alice, "line " + i);
        Assert.That(bot.Transport.LastText(Alice), Does.Contain("<code>12:34</code>"));

        await bot.Text(Alice, "/settings");
        var settings = bot.Page<SettingsViewModel>(Alice);
        Assert.That(settings.PageSize.SelectedValue, Is.EqualTo(8));
        await bot.Click(Alice, settings.Timestamps.CallbackData);
        Assert.That(settings.Timestamps.DisplayValue, Is.EqualTo("Hidden"));
        await bot.Click(Alice, settings.PageSize.CallbackData);
        await bot.Click(Alice, bot.ChoiceCallback(bot.Page<PickerDialogViewModel<int>>(Alice), 0));
        await bot.Text(Alice, "/back");

        Assert.That(bot.Current(Alice), Is.SameAs(chat));
        Assert.That(chat.History.GetWindow().Items, Has.Count.EqualTo(5));
        Assert.That(bot.Transport.LastText(Alice), Does.Not.Contain("<code>12:34</code>"));
    }

    [Test]
    public async Task LeavingAnnouncesDepartureAndPeoplePageCountsMessages()
    {
        using var bot = new Harness();
        await bot.Enter(Alice, "Alice");
        await bot.Enter(Bob, "Bob");
        await bot.Text(Bob, "hello hato");
        Assert.That(bot.Room.Messages.Last().Kind, Is.EqualTo(ChatMessageKind.Bot), "the pigeon answers");

        await bot.Text(Alice, "/back");
        Assert.That(bot.Current(Alice), Is.TypeOf<LobbyViewModel>());
        Assert.That(bot.Room.Online.Select(m => m.Nickname), Is.EqualTo(new[] { "Bob" }));
        Assert.That(bot.Room.Messages.Last(), Has.Property("Kind").EqualTo(ChatMessageKind.System)
            .And.Property("Author").EqualTo("Alice"));
        // Alice's lobby watches the room too and learns about Bob's next message.
        await bot.Text(Bob, "/shrug");
        await bot.Refresh.ProcessPendingAsync();
        Assert.That(bot.Transport.LastText(Alice), Does.Contain(@"¯\_(ツ)_/¯"));

        await bot.Text(Bob, "/people");
        var people = bot.Page<PeopleViewModel>(Bob);
        var page = new PeopleView().Display(people).ResponseMessages.Single().text;
        Assert.That(page, Does.Contain("🥇 Bob — 2 msg").And.Contain("<b>1</b> online"));
        await bot.Text(Bob, "/back");
        Assert.That(bot.Current(Bob), Is.TypeOf<ChatRoomViewModel>());
    }

    [Test]
    public async Task LongHistoryStaysWithinTelegramMessageLimit()
    {
        using var bot = new Harness();
        var chat = await bot.Enter(Alice, "Alice");
        chat.History.PageSize = 12;
        var noisy = new string('&', ChatCommands.MaxLength);
        for (var i = 0; i < 12; i++) bot.Room.Post(chat.Me, new ChatPost(ChatMessageKind.Say, noisy));
        var text = new ChatRoomView().Display(chat).ResponseMessages.Single().text;
        Assert.That(text.Length, Is.LessThan(4096));
        Assert.That(text, Does.Contain("earlier"));
    }

    [TestCase("hello", ChatMessageKind.Say, "hello")]
    [TestCase("/me dances", ChatMessageKind.Action, "dances")]
    [TestCase("/hug Bob", ChatMessageKind.Action, "hugs Bob 🤗")]
    [TestCase("/shrug ok", ChatMessageKind.Say, @"ok ¯\_(ツ)_/¯")]
    public void CommandsInterpretText(string input, ChatMessageKind kind, string text)
    {
        Assert.That(ChatCommands.Interpret(input, new Random(1)), Is.EqualTo(new ChatPost(kind, text)));
    }

    [Test]
    public void DiceStayInRangeAndUnknownCommandsAreRejected()
    {
        var random = new Random(7);
        for (var i = 0; i < 200; i++)
        {
            var roll = ChatCommands.Interpret("/roll 3", random)!.Text;
            Assert.That(roll, Does.Match(@"^rolled [123] on a d3"));
        }
        Assert.That(ChatCommands.Interpret("/roll banana", random)!.Text, Does.Contain("d6"));
        Assert.That(ChatCommands.Interpret("/me", random), Is.Null);
        Assert.That(ChatCommands.Interpret("/teleport", random), Is.Null);
        Assert.That(ChatCommands.Validate("   "), Is.Not.Null);
    }

    private sealed class Harness : IDisposable
    {
        private readonly HttpClient _http;
        private readonly TelegramBotClient _client;
        private readonly TestBotHost _app;
        private readonly SessionManager _sessions;
        private int _nextMessageId = 1000;
        public FakeTransport Transport { get; } = new();
        public ChatRoom Room { get; }
        public NicknameRegistry Registry { get; }
        public ViewRefreshQueue Refresh { get; }

        public Harness()
        {
            _http = new HttpClient(Transport);
            _client = new TelegramBotClient("123456:offline-test-token", _http);
            var dependencies = new DependencyManager();
            _app = new TestBotHost("123456:offline-test-token");
            dependencies.InjectDependencies(_app);
            T Get<T>() => (T)dependencies.GetDependencyInstance(typeof(T), new InjectAttribute());
            Get<TelegramInteractionManager>()._telegramBotClient = _client;
            _sessions = Get<SessionManager>();
            Room = Get<ChatRoom>();
            Room.Random = new Random(42);
            Room.Clock = () => new DateTimeOffset(2026, 10, 26, 12, 34, 0, TimeSpan.Zero);
            Registry = Get<NicknameRegistry>();
            Refresh = Get<ViewRefreshQueue>();
        }

        public TgState Current(long user) => _sessions.GetCurrentContext(user, user).CurrentState!;
        public T Page<T>(long user) => (T)(object)Current(user);

        public Task Text(long user, string text) => _app.HandleUpdateAsync(_client, new Update
        {
            Message = new Message
            {
                MessageId = ++_nextMessageId, From = new User { Id = user },
                Chat = new Chat { Id = user, Type = ChatType.Private }, Text = text
            }
        }, CancellationToken.None);

        public Task Click(long user, string data) => _app.HandleUpdateAsync(_client, new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "button", From = new User { Id = user }, Data = data,
                Message = new Message { Chat = new Chat { Id = user, Type = ChatType.Private } }
            }
        }, CancellationToken.None);

        public async Task Register(long user, string nickname)
        {
            await Click(user, Page<LobbyViewModel>(user).Nickname.CallbackData);
            await Text(user, nickname);
        }

        public async Task<ChatRoomViewModel> Enter(long user, string nickname)
        {
            await Text(user, "/start");
            await Register(user, nickname);
            await Click(user, Page<LobbyViewModel>(user).Enter.CallbackData);
            return Page<ChatRoomViewModel>(user);
        }

        public string ChoiceCallback<T>(PickerDialogViewModel<T> picker, int index) =>
            new PickerDialogView().Display(picker).ResponseMessages.Single().replyMarkup!
                .InlineKeyboard.ElementAt(index).Single().CallbackData!;

        public void Dispose() => _http.Dispose();
    }

    private sealed class FakeTransport : HttpMessageHandler
    {
        private int _messageId;
        public ConcurrentQueue<(string Method, long ChatId, string? Text)> Calls { get; private set; } = new();

        public string LastText(long chatId) => Calls.Last(c => c.ChatId == chatId && c.Text != null).Text!;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var method = request.RequestUri!.Segments.Last();
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var chatId = body.RootElement.TryGetProperty("chat_id", out var chat) ? long.Parse(chat.ToString()) : 0;
            var text = body.RootElement.TryGetProperty("text", out var value) ? value.GetString() : null;
            Calls.Enqueue((method, chatId, text));
            var result = method is "answerCallbackQuery" or "deleteMessage" ? "true"
                : "{\"message_id\":" + Interlocked.Increment(ref _messageId) + ",\"date\":0,\"chat\":{\"id\":" + chatId + ",\"type\":\"private\"}}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ok\":true,\"result\":" + result + "}", Encoding.UTF8, "application/json")
            };
        }
    }
}
