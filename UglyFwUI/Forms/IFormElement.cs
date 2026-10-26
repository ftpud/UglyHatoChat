namespace UglyFwUI.Forms;

public interface IFormElement
{
    string Label { get; }
    string CallbackData { get; }
    // Null identifies an action button; an empty value identifies an unfilled field.
    string? DisplayValue { get; }
}
