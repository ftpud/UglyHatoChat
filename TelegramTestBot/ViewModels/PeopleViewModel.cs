using Telegram.Bot.Types;
using TelegramTestBot.Models;
using TelegramTestBot.Services;
using TelegramTestBot.Views;
using UglyAppFramework.DependencyManager.Attributes;
using UglyTgApplication.Attributes;

namespace TelegramTestBot.ViewModels;

[BindView(typeof(PeopleView))]
public class PeopleViewModel : UglyTgApplication.States.ViewModel
{
    [Inject] private ChatRoom Room { get; set; } = null!;

    public IReadOnlyList<Chatter> Online => Room.Online;
    public IReadOnlyList<(string Nickname, int Count)> Leaderboard => Room.Leaderboard;
    public int TotalMessages => Room.Messages.Count;

    [Callback(Trigger = "/back")]
    public Task Back(Update update, CancellationToken cancellationToken) => PopAsync(cancellationToken);

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
}
