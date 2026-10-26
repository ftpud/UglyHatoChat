using TelegramTestBot.Services;
using TelegramTestBot.ViewModels;
using UglyAppFramework.DependencyManager.Attributes;
using UglyAppFramework.StateManage;
using UglyFwUI.Rendering;
using UglyTgApplication.View;

namespace TelegramTestBot.Views;

[Managed]
public class PeopleView : IView
{
    private static readonly string[] Medals = { "🥇", "🥈", "🥉", "🏅", "🏅" };

    public ViewResponse Display(IState viewModel)
    {
        var model = (PeopleViewModel)viewModel;
        var leaderboard = model.Leaderboard.Count == 0
            ? "<i>Nobody has said anything yet. Suspicious.</i>"
            : string.Join("\n", model.Leaderboard.Select((entry, i) =>
                $"{Medals[i]} {HtmlText.Encode(entry.Nickname)} — {entry.Count} msg"));
        return FormLayout.Create("👥 Who's here")
            .Html(ChatFormat.Members(model.Online))
            .Html($"🏆 <b>Chattiest</b> ({model.TotalMessages} messages in history)\n" + leaderboard)
            .Html("🪄 <b>Tricks</b>\n" + HtmlText.Encode(ChatCommands.Help))
            .Back("/back")
            .Build();
    }
}
