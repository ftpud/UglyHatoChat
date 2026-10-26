using TelegramTestBot.ViewModels;
using UglyAppFramework.DependencyManager.Attributes;
using UglyAppFramework.StateManage;
using UglyFwUI.Rendering;
using UglyTgApplication.View;

namespace TelegramTestBot.Views;

[Managed]
public class ChatRoomView : IView
{
    public ViewResponse Display(IState viewModel)
    {
        var model = (ChatRoomViewModel)viewModel;
        return FormLayout.Create($"💬 HatoChat · you are {model.Me.Avatar} {model.Me.Nickname}")
            .Html(ChatFormat.Members(model.Online))
            .Scroll(model.History, message => ChatFormat.Message(message, model.Preferences, model.Me.Nickname),
                "No messages yet. Break the ice 🧊🔨", maxLength: 2800)
            .Html("<i>" + HtmlText.Encode(model.Status) + "</i>")
            .Fields(model.Fields)
            .Buttons(model.QuickActions.Select(b => (b.Label, b.CallbackData)).ToArray())
            .Buttons(("⚙️ Settings", "/settings"), ("👥 People", "/people"), ("🚪 Leave", "/back"))
            .Build();
    }
}
