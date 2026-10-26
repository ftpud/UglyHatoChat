using TelegramTestBot.ViewModels;
using UglyAppFramework.DependencyManager.Attributes;
using UglyAppFramework.StateManage;
using UglyFwUI.Rendering;
using UglyTgApplication.View;

namespace TelegramTestBot.Views;

[Managed]
public class SettingsView : IView
{
    public ViewResponse Display(IState viewModel)
    {
        var model = (SettingsViewModel)viewModel;
        return FormLayout.Create("⚙️ Settings")
            .Text("Tweak how the chat looks. Changes apply when you go back.")
            .Fields(model.Fields)
            .Back("/back")
            .Build();
    }
}
