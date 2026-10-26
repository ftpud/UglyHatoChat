using UglyTgApplication.States;

namespace UglyFwUI.Forms;

public class FormButton<T> : IFormElement
{
    public string Label { get; }
    public string CallbackData { get; } = CallbackId.Create();
    public T Data { get; set; }
    public string? DisplayValue => null;

    public FormButton(ViewModel owner, string label, T data, Func<T, CancellationToken, Task> onClick)
    {
        Label = label;
        Data = data;
        owner.RegisterCallback(CallbackData, (_, token) => onClick(Data, token));
    }

}
