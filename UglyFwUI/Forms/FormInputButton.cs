using UglyFwUI.ViewModels;
using UglyTgApplication.States;

namespace UglyFwUI.Forms;

// One-shot input: opens a text dialog and hands the accepted text to onSubmit without
// storing it, e.g. "send a message". Use FormTextBox for a field that keeps its value.
public class FormInputButton : IFormElement
{
    public string Label { get; }
    public string CallbackData { get; } = CallbackId.Create();
    public string? DisplayValue => null;

    public FormInputButton(ViewModel owner, string label, string prompt,
        Func<string, CancellationToken, Task> onSubmit, Func<string, string?>? validate = null)
    {
        Label = label;
        owner.RegisterCallback(CallbackData, (_, token) =>
            owner.PushAsync(new TextDialogViewModel(prompt, onSubmit, validate), token));
    }
}
