using TelegramTestBot.ViewModels;
using UglyAppFramework.DependencyManager.Attributes;
using UglyAppFramework.StateManage;
using UglyFwUI.Rendering;
using UglyTgApplication.View;

namespace TelegramTestBot.Views;

[Managed]
public class LobbyView : IView
{
    private const string Banner =
        "<pre>  __( o)>   HatoChat\n  \\ &lt;_ )    the ugliest chat\n   `---'    on Telegram</pre>";

    public ViewResponse Display(IState viewModel)
    {
        var model = (LobbyViewModel)viewModel;
        var latest = model.Latest is { } message
            ? "📣 Latest: " + ChatFormat.Message(message, model.Preferences)
            : "📣 Quiet in here… be the first to say hi!";
        return FormLayout.Create("🐦 HatoChat lobby")
            .Html(Banner)
            .Html(ChatFormat.Members(model.Online) + "\n" + latest)
            .Html("<i>" + HtmlText.Encode(model.Status) + "</i>")
            .Fields(model.Fields)
            .Build();
    }
}
