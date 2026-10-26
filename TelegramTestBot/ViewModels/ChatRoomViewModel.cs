using Telegram.Bot.Types;
using TelegramTestBot.Models;
using TelegramTestBot.Services;
using TelegramTestBot.Views;
using UglyAppFramework.DependencyManager.Attributes;
using UglyFwUI.Forms;
using UglyTgApplication.Attributes;

namespace TelegramTestBot.ViewModels;

[BindView(typeof(ChatRoomView))]
public class ChatRoomViewModel : UglyTgApplication.States.ViewModel
{
    public const string DefaultStatus = "Just type to chat, or tap 💬 Message. /help for tricks.";

    [Inject] private ChatRoom Room { get; set; } = null!;

    public Chatter Me { get; }
    public ChatPreferences Preferences { get; }
    public FormScrollBox<ChatMessage> History { get; }
    public FormInputButton Compose { get; }
    public IReadOnlyList<IFormElement> Fields { get; }
    public IReadOnlyList<FormButton<string>> QuickActions { get; }
    public string Status { get; private set; } = DefaultStatus;
    public IReadOnlyList<Chatter> Online => Room.Online;

    public ChatRoomViewModel(Chatter me, ChatPreferences preferences)
    {
        Me = me;
        Preferences = preferences;
        History = new FormScrollBox<ChatMessage>(this, () => Room.Messages, preferences.PageSize);
        // The dialog rejects bad input in place; once it pops, the chat renders the new message.
        Compose = new FormInputButton(this, "💬 Message", "💬 Type your message (or a /trick):", (text, _) =>
        {
            Say(text);
            return Task.CompletedTask;
        }, ChatCommands.ValidateMessage);
        Fields = Array.AsReadOnly(new IFormElement[] { Compose });
        QuickActions = Array.AsReadOnly(new[]
        {
            QuickAction("🎲 Roll", "/roll"), QuickAction("🪙 Flip", "/flip"), QuickAction("👋 Wave", "/me waves 👋")
        });
    }

    [Callback(Trigger = "/settings")]
    public Task OpenSettings(Update update, CancellationToken cancellationToken) =>
        PushAsync(new SettingsViewModel(Preferences), cancellationToken);

    [Callback(Trigger = "/people")]
    public Task OpenPeople(Update update, CancellationToken cancellationToken) =>
        PushAsync(new PeopleViewModel(), cancellationToken);

    [Callback(Trigger = "/back")]
    public Task Leave(Update update, CancellationToken cancellationToken) => PopAsync(cancellationToken);

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Room.Watch(this);
        Room.Join(Me, this);
        await base.InitializeAsync(cancellationToken);
    }

    public override async Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        History.PageSize = Preferences.PageSize;
        await base.ActivateAsync(cancellationToken);
    }

    public override async Task UnloadAsync(CancellationToken cancellationToken = default)
    {
        Room.Unwatch(this);
        Room.Leave(Me.UserId, this);
        await base.UnloadAsync(cancellationToken);
    }

    public override async Task OnMessageAsync(Update update, CancellationToken cancellationToken = default)
    {
        if (update.Message?.Text is not { } text || text.StartsWith("/start")) return;
        Say(text);
        await UpdateViewAsync(cancellationToken);
    }

    // Posts text or a fun command; returns false and explains why when nothing was posted.
    public bool Say(string text)
    {
        if (ChatCommands.IsHelp(text))
        {
            Status = ChatCommands.Help;
            return false;
        }
        if (ChatCommands.Validate(text) is { } error)
        {
            Status = error;
            return false;
        }
        if (Room.Interpret(text) is not { } post)
        {
            Status = "Unknown command 🤔 Try /help.";
            return false;
        }
        Room.Post(Me, post, this);
        // Your own message is always worth seeing.
        History.ScrollToLatest();
        Status = DefaultStatus;
        return true;
    }

    private FormButton<string> QuickAction(string label, string command) =>
        new(this, label, command, (data, token) =>
        {
            Say(data);
            return UpdateViewAsync(token);
        });
}
