using UglyFwUI.ViewModels;
using UglyTgApplication.States;

namespace UglyFwUI.Forms;

public class FormTextBox : IFormElement
{
    public string Label { get; }
    public string CallbackData { get; } = CallbackId.Create();
    public string Text { get; private set; }
    public string DisplayValue => Text;

    public FormTextBox(ViewModel owner, string label, string text = "",
        Func<string, string?>? validate = null, Func<string, CancellationToken, Task>? onChanged = null)
    {
        Label = label;
        Text = text;
        owner.RegisterCallback(CallbackData, (_, token) => owner.PushAsync(new TextDialogViewModel(label,
            async (value, submitToken) =>
            {
                if (onChanged != null) await onChanged(value, submitToken);
                Text = value;
            }, validate), token));
    }

    // Lets the owner reset a consumed value.
    public void Clear() => Text = string.Empty;

}
