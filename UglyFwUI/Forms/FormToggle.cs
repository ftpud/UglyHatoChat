using UglyTgApplication.States;

namespace UglyFwUI.Forms;

public class FormToggle : IFormElement
{
    private readonly string _onText;
    private readonly string _offText;
    public string Label { get; }
    public string CallbackData { get; } = CallbackId.Create();
    public bool Value { get; private set; }
    public string DisplayValue => Value ? _onText : _offText;

    public FormToggle(ViewModel owner, string label, bool value = false,
        Func<bool, CancellationToken, Task>? onChanged = null, string onText = "On", string offText = "Off")
    {
        Label = label;
        Value = value;
        _onText = onText;
        _offText = offText;
        owner.RegisterCallback(CallbackData, async (_, token) =>
        {
            var next = !Value;
            if (onChanged != null) await onChanged(next, token);
            Value = next;
            await owner.UpdateViewAsync(token);
        });
    }
}
