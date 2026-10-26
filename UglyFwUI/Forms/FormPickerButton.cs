using UglyFwUI.Models;
using UglyFwUI.ViewModels;
using UglyTgApplication.States;

namespace UglyFwUI.Forms;

public class FormPickerButton<T> : IFormElement
{
    public string Label { get; }
    public string CallbackData { get; } = CallbackId.Create();
    public T? SelectedValue { get; private set; }
    public string SelectedText { get; private set; } = string.Empty;
    public bool HasSelection { get; private set; }
    public string DisplayValue => SelectedText;
    public IReadOnlyList<PickerOption<T>> Options { get; }

    public FormPickerButton(ViewModel owner, string label, IEnumerable<PickerOption<T>> options,
        Func<T, CancellationToken, Task>? onChanged = null)
    {
        Label = label;
        Options = Array.AsReadOnly(options.ToArray());
        owner.RegisterCallback(CallbackData, (_, token) => owner.PushAsync(new PickerDialogViewModel<T>(label,
            Options, async (option, selectToken) =>
            {
                if (onChanged != null) await onChanged(option.Value, selectToken);
                SelectedValue = option.Value;
                SelectedText = option.Label;
                HasSelection = true;
            }), token));
    }

    // Preselects the first option with this value; returns false when no option matches.
    public bool Select(T value)
    {
        var option = Options.FirstOrDefault(o => EqualityComparer<T>.Default.Equals(o.Value, value));
        if (option == null) return false;
        SelectedValue = option.Value;
        SelectedText = option.Label;
        HasSelection = true;
        return true;
    }

}
