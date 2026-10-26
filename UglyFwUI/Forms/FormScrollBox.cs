using UglyFwUI.Models;
using UglyTgApplication.States;

namespace UglyFwUI.Forms;

// A page-sized window over a growing list. In auto mode it follows the newest items;
// scrolling up switches to manual mode, which stays put while new items arrive.
public class FormScrollBox<T>
{
    private readonly Func<IReadOnlyList<T>> _source;
    private int _pageSize;
    private int _top;

    public string OldestCallbackData { get; } = CallbackId.Create();
    public string OlderCallbackData { get; } = CallbackId.Create();
    public string NewerCallbackData { get; } = CallbackId.Create();
    public string LatestCallbackData { get; } = CallbackId.Create();
    public string AutoScrollCallbackData { get; } = CallbackId.Create();
    public bool AutoScroll { get; private set; } = true;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    public FormScrollBox(ViewModel owner, Func<IReadOnlyList<T>> source, int pageSize = 8)
    {
        _source = source;
        PageSize = pageSize;
        owner.RegisterCallback(OldestCallbackData, (_, token) => Scroll(owner, ScrollToOldest, token));
        owner.RegisterCallback(OlderCallbackData, (_, token) => Scroll(owner, ScrollOlder, token));
        owner.RegisterCallback(NewerCallbackData, (_, token) => Scroll(owner, ScrollNewer, token));
        owner.RegisterCallback(LatestCallbackData, (_, token) => Scroll(owner, ScrollToLatest, token));
        owner.RegisterCallback(AutoScrollCallbackData, (_, token) => Scroll(owner, ToggleAutoScroll, token));
    }

    public ScrollWindow<T> GetWindow()
    {
        var items = _source();
        var start = Start(items.Count);
        var count = Math.Min(PageSize, items.Count - start);
        var visible = new T[count];
        for (var i = 0; i < count; i++) visible[i] = items[start + i];
        return new ScrollWindow<T>(Array.AsReadOnly(visible), start, items.Count - start - count, AutoScroll);
    }

    public void ScrollOlder()
    {
        _top = Math.Max(0, Start(_source().Count) - PageSize);
        AutoScroll = false;
    }

    public void ScrollNewer()
    {
        if (AutoScroll) return;
        var total = _source().Count;
        _top = Start(total) + PageSize;
        // Reaching the bottom sticks to it again, like most chat apps.
        if (_top >= LastStart(total)) AutoScroll = true;
    }

    public void ScrollToOldest()
    {
        _top = 0;
        AutoScroll = _source().Count <= PageSize;
    }

    public void ScrollToLatest() => AutoScroll = true;

    public void ToggleAutoScroll()
    {
        // Freezing keeps the currently visible items in place.
        if (AutoScroll) _top = Start(_source().Count);
        AutoScroll = !AutoScroll;
    }

    private int LastStart(int total) => Math.Max(0, total - PageSize);
    private int Start(int total) => AutoScroll ? LastStart(total) : Math.Clamp(_top, 0, LastStart(total));

    private static Task Scroll(ViewModel owner, Action scroll, CancellationToken token)
    {
        scroll();
        return owner.UpdateViewAsync(token);
    }
}
