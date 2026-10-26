using System.Text;

namespace UglyFwUI.Rendering;

public static class HtmlText
{
    // Telegram HTML only needs these escaped. Emoji and other text stay as-is, so
    // messages don't grow into numeric entities and stay within length limits.
    public static string Encode(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
            builder.Append(c switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                '"' => "&quot;",
                _ => c.ToString()
            });
        return builder.ToString();
    }
}
