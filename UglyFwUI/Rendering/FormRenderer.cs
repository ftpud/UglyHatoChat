using UglyFwUI.Forms;
using UglyTgApplication.View;

namespace UglyFwUI.Rendering;

public static class FormRenderer
{
    public static ViewResponse Render(string title, IEnumerable<IFormElement> elements, string? backCallbackData = null)
    {
        var layout = FormLayout.Create(title).Fields(elements);
        return backCallbackData == null ? layout.Build() : layout.Back(backCallbackData).Build();
    }
}
