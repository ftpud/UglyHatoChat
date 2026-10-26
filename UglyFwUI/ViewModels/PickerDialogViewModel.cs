using Telegram.Bot.Types;
using UglyFwUI.Forms;
using UglyFwUI.Models;
using UglyFwUI.Views;
using UglyTgApplication.Attributes;

namespace UglyFwUI.ViewModels;

internal interface IPickerDialogState
{
    string Prompt { get; }
    string BackCallbackData { get; }
    IReadOnlyList<(string Label, string CallbackData)> Choices { get; }
}

[BindView(typeof(PickerDialogView))]
public class PickerDialogViewModel<T> : UglyTgApplication.States.ViewModel, IPickerDialogState
{
    public string Prompt { get; }
    public string BackCallbackData { get; } = CallbackId.Create();
    public IReadOnlyList<PickerOption<T>> Options { get; }
    private readonly List<(string Label, string CallbackData)> _choices = new();
    IReadOnlyList<(string Label, string CallbackData)> IPickerDialogState.Choices => _choices;

    public PickerDialogViewModel(string prompt, IEnumerable<PickerOption<T>> options,
        Func<PickerOption<T>, CancellationToken, Task> onSelect)
    {
        Prompt = prompt;
        Options = Array.AsReadOnly(options.ToArray());
        RemoveUserMessagesPolicyEnabled = false;
        RegisterCallback(BackCallbackData, (_, token) => PopAsync(token));
        foreach (var option in Options)
        {
            var action = CallbackId.Create();
            _choices.Add((option.Label, action));
            RegisterCallback(action, async (_, token) =>
            {
                await onSelect(option, token);
                await PopAsync(token);
            });
        }
    }

    [Callback(Trigger = "/back")]
    public Task Back(Update update, CancellationToken token) => PopAsync(token);
}
