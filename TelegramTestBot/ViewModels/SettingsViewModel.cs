using Telegram.Bot.Types;
using TelegramTestBot.Models;
using TelegramTestBot.Views;
using UglyFwUI.Forms;
using UglyFwUI.Models;
using UglyTgApplication.Attributes;

namespace TelegramTestBot.ViewModels;

[BindView(typeof(SettingsView))]
public class SettingsViewModel : UglyTgApplication.States.ViewModel
{
    public FormToggle Timestamps { get; }
    public FormToggle Mentions { get; }
    public FormPickerButton<int> PageSize { get; }
    public IReadOnlyList<IFormElement> Fields { get; }

    public SettingsViewModel(ChatPreferences preferences)
    {
        Timestamps = new FormToggle(this, "🕒 Timestamps", preferences.ShowTimestamps, (value, _) =>
        {
            preferences.ShowTimestamps = value;
            return Task.CompletedTask;
        }, "Shown", "Hidden");
        Mentions = new FormToggle(this, "🔔 Highlight @mentions", preferences.HighlightMentions, (value, _) =>
        {
            preferences.HighlightMentions = value;
            return Task.CompletedTask;
        }, "Loud", "Quiet");
        PageSize = new FormPickerButton<int>(this, "📏 Messages per page", new[]
        {
            new PickerOption<int>("🤏 Cozy (5)", 5), new PickerOption<int>("🛋 Comfy (8)", 8),
            new PickerOption<int>("🐋 Chonky (12)", 12)
        }, (value, _) =>
        {
            preferences.PageSize = value;
            return Task.CompletedTask;
        });
        PageSize.Select(preferences.PageSize);
        Fields = Array.AsReadOnly(new IFormElement[] { Timestamps, Mentions, PageSize });
    }

    [Callback(Trigger = "/back")]
    public Task Back(Update update, CancellationToken cancellationToken) => PopAsync(cancellationToken);
}
