namespace UglyFwUI.Forms;

internal static class CallbackId
{
    // Per-instance IDs keep old keyboards from activating controls on a new page.
    public static string Create() => "ui:" + Guid.NewGuid().ToString("N");
}
