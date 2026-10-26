using Telegram.Bot.Types.ReplyMarkups;
using UglyFwUI.Forms;
using UglyTgApplication.View;

namespace UglyFwUI.Rendering;

// Composes one form message from sections (paragraphs, scroll boxes, fields) and keyboard rows.
public sealed class FormLayout
{
    private readonly string _title;
    private readonly List<string> _sections = new();
    private readonly List<InlineKeyboardButton[]> _rows = new();

    private FormLayout(string title) => _title = title;

    public static FormLayout Create(string title) => new(title);

    // The caller is responsible for escaping dynamic values inside html.
    public FormLayout Html(string html)
    {
        _sections.Add(html);
        return this;
    }

    public FormLayout Text(string text) => Html(HtmlText.Encode(text));

    // formatHtml must escape item content. maxLength bounds the section so a page of
    // long items stays within Telegram's 4096-character message limit.
    public FormLayout Scroll<T>(FormScrollBox<T> box, Func<T, string> formatHtml,
        string emptyText = "Nothing here yet.", int maxLength = 3000)
    {
        var window = box.GetWindow();
        var lines = new List<string>();
        var length = 0;
        var trimmed = 0;
        for (var i = window.Items.Count - 1; i >= 0; i--)
        {
            var line = formatHtml(window.Items[i]);
            if (lines.Count > 0 && length + line.Length + 1 > maxLength)
            {
                trimmed = i + 1;
                break;
            }
            length += line.Length + 1;
            lines.Insert(0, line);
        }

        var older = window.Older + trimmed;
        var body = new List<string>();
        if (older > 0) body.Add($"<i>▲ {older} earlier</i>");
        body.Add(lines.Count == 0 ? "<i>" + HtmlText.Encode(emptyText) + "</i>" : string.Join("\n", lines));
        if (window.Newer > 0) body.Add($"<i>▼ {window.Newer} newer</i>");
        _sections.Add(string.Join("\n", body));

        _rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("⏫", box.OldestCallbackData),
            InlineKeyboardButton.WithCallbackData("🔼", box.OlderCallbackData),
            InlineKeyboardButton.WithCallbackData("🔽", box.NewerCallbackData),
            InlineKeyboardButton.WithCallbackData("⏬", box.LatestCallbackData),
            InlineKeyboardButton.WithCallbackData(window.AutoScroll ? "📌 Auto" : "✋ Manual", box.AutoScrollCallbackData)
        });
        return this;
    }

    public FormLayout Fields(IEnumerable<IFormElement> elements)
    {
        var snapshot = elements.ToArray();
        _sections.Add(string.Join("\n", snapshot.Select(element =>
            element.DisplayValue is { } value
                ? $"{HtmlText.Encode(element.Label)}: <code>{HtmlText.Encode(value)}</code>"
                : HtmlText.Encode(element.Label))));
        _rows.AddRange(snapshot.Select(element => new[]
        {
            InlineKeyboardButton.WithCallbackData(element.Label, element.CallbackData)
        }));
        return this;
    }

    public FormLayout Buttons(params (string Label, string CallbackData)[] row)
    {
        if (row.Length > 0)
            _rows.Add(row.Select(b => InlineKeyboardButton.WithCallbackData(b.Label, b.CallbackData)).ToArray());
        return this;
    }

    public FormLayout Back(string callbackData, string label = "Back") => Buttons((label, callbackData));

    public ViewResponse Build() => new ViewSimpleResponse(
        "<b>" + HtmlText.Encode(_title) + "</b>\n\n" + string.Join("\n\n", _sections),
        new InlineKeyboardMarkup(_rows));
}
