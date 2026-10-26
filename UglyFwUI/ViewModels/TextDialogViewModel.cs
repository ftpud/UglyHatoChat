using Telegram.Bot.Types;
using UglyFwUI.Forms;
using UglyFwUI.Views;
using UglyTgApplication.Attributes;

namespace UglyFwUI.ViewModels;

[BindView(typeof(TextDialogView))]
public class TextDialogViewModel : UglyTgApplication.States.ViewModel
{
    private readonly Func<string, CancellationToken, Task> _onSubmit;
    private readonly Func<string, string?> _validate;
    public string Prompt { get; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public string BackCallbackData { get; } = CallbackId.Create();

    public TextDialogViewModel(string prompt, Func<string, CancellationToken, Task> onSubmit,
        Func<string, string?>? validate = null)
    {
        Prompt = prompt;
        _onSubmit = onSubmit;
        _validate = validate ?? (_ => null);
        RemoveUserMessagesPolicyEnabled = false;
        RegisterCallback(BackCallbackData, (_, token) => PopAsync(token));
    }

    [Callback(Trigger = "/back")]
    public Task Back(Update update, CancellationToken token) => PopAsync(token);

    public override async Task OnMessageAsync(Update update, CancellationToken cancellationToken = default)
    {
        if (update.Message?.Text is not { } text || text == "/back") return;
        var error = _validate(text);
        if (!string.IsNullOrEmpty(error))
        {
            ErrorMessage = error;
            await UpdateViewAsync(cancellationToken);
            return;
        }
        // Update the parent model before PopAsync reactivates and renders it.
        await _onSubmit(text, cancellationToken);
        await PopAsync(cancellationToken);
    }
}
