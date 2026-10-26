using Telegram.Bot.Types;
using TelegramTestBot.Models;
using TelegramTestBot.Services;
using TelegramTestBot.Views;
using UglyAppFramework.DependencyManager.Attributes;
using UglyFwUI.Forms;
using UglyFwUI.Models;
using UglyTgApplication.Attributes;

namespace TelegramTestBot.ViewModels;

[BindView(typeof(LobbyView))]
public class LobbyViewModel : UglyTgApplication.States.ViewModel
{
    public const string DefaultAvatar = "🙂";

    [Inject] private NicknameRegistry Registry { get; set; } = null!;
    [Inject] private ChatRoom Room { get; set; } = null!;

    public ChatPreferences Preferences { get; } = new();
    public FormTextBox Nickname { get; }
    public FormPickerButton<string> Avatar { get; }
    public FormButton<string> Enter { get; }
    public IReadOnlyList<IFormElement> Fields { get; }
    public string Status { get; private set; } = "Register a nickname, pick an avatar, then jump in!";
    public IReadOnlyList<Chatter> Online => Room.Online;
    public ChatMessage? Latest => Room.Messages.LastOrDefault();

    public LobbyViewModel()
    {
        // The validator claims the name atomically, so two users cannot both pass for the same name.
        Nickname = new FormTextBox(this, "🏷 Nickname", validate: name => Registry.TryClaim(Context.UserId, name),
            onChanged: (name, _) =>
            {
                Status = $"Nice to meet you, {name}! 🎉";
                return Task.CompletedTask;
            });
        Avatar = new FormPickerButton<string>(this, "🎭 Avatar", new[]
        {
            new PickerOption<string>("🦊 Fox", "🦊"), new PickerOption<string>("🐸 Frog", "🐸"),
            new PickerOption<string>("🐼 Panda", "🐼"), new PickerOption<string>("🐙 Octopus", "🐙"),
            new PickerOption<string>("🦄 Unicorn", "🦄"), new PickerOption<string>("🤖 Robot", "🤖"),
            new PickerOption<string>("👻 Ghost", "👻"), new PickerOption<string>("🥔 Potato", "🥔")
        });
        Enter = new FormButton<string>(this, "🚪 Enter the chat", string.Empty, async (_, token) =>
        {
            if (Registry.Get(Context.UserId) is not { } nickname)
            {
                Status = "Who goes there?! Register a nickname first 🛂";
                await UpdateViewAsync(token);
                return;
            }
            var member = new Chatter(Context.UserId, nickname, Avatar.HasSelection ? Avatar.SelectedValue! : DefaultAvatar);
            await PushAsync(new ChatRoomViewModel(member, Preferences), token);
        });
        var settings = new FormButton<string>(this, "⚙️ Settings", string.Empty,
            (_, token) => PushAsync(new SettingsViewModel(Preferences), token));
        var people = new FormButton<string>(this, "👥 Who's here", string.Empty,
            (_, token) => PushAsync(new PeopleViewModel(), token));
        Fields = Array.AsReadOnly(new IFormElement[] { Nickname, Avatar, Enter, settings, people });
    }

    [Callback(Trigger = "/start")]
    public Task Start(Update update, CancellationToken cancellationToken) => UpdateViewAsync(cancellationToken);

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Room.Watch(this);
        await base.InitializeAsync(cancellationToken);
    }

    public override async Task UnloadAsync(CancellationToken cancellationToken = default)
    {
        Room.Unwatch(this);
        await base.UnloadAsync(cancellationToken);
    }

    public override async Task OnMessageAsync(Update update, CancellationToken cancellationToken = default)
    {
        if (update.Message?.Text is not { } text || text.StartsWith("/start")) return;
        Status = "Psst… the chatting happens behind the 🚪 door.";
        await UpdateViewAsync(cancellationToken);
    }
}
