namespace UglyFwUI.Models;

// Visible slice of a scroll box: Older/Newer count the items hidden above/below.
public sealed record ScrollWindow<T>(IReadOnlyList<T> Items, int Older, int Newer, bool AutoScroll);
