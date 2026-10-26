# UglyFwUI

Reusable Telegram dialogs and form controls for the async UglyAppFw framework.
Reference `UglyFwUI.csproj` from an application; the library references
`../UglyAppFw/UglyTgApplication` and has no application or game dependencies.

The controls were reworked from the patterns in
[IdolManGame's GameShared/UI](https://github.com/ftpud/IdolManGame/tree/6de995cab564e9e1eda8f3ca61540a3778b707c1/GameShared/UI).
The reusable dialogs and form elements are gathered here with awaited callbacks,
typed picker values, per-instance callback IDs, and HTML escaping. Game-specific
`UiHelper` entity/card formatters remain application responsibilities.

| Folder | Contents |
| --- | --- |
| `Views/` | Stateless managed text and picker renderers |
| `ViewModels/` | Text and generic picker dialog state/navigation |
| `Models/` | `PickerOption<T>` label/value record, `ScrollWindow<T>` |
| `Forms/` | `IFormElement`, `FormButton<T>`, `FormTextBox`, `FormPickerButton<T>`, `FormInputButton`, `FormToggle`, `FormScrollBox<T>` |
| `Rendering/` | `HtmlText` escaping, `FormLayout` composition and `FormRenderer` shortcut |
| `Tests/` | Offline control and navigation behavior tests |

## Dialogs

Push a dialog from a parent view model's awaited handler:

```csharp
using UglyFwUI.Models;
using UglyFwUI.ViewModels;

await PushAsync(new TextDialogViewModel("Enter name:", (text, token) =>
{
    Name = text;
    return Task.CompletedTask;
}, text => string.IsNullOrWhiteSpace(text) ? "Name is required." : null), cancellationToken);

await PushAsync(new PickerDialogViewModel<int>("Select a size:", new[]
{
    new PickerOption<int>("Small", 1), new PickerOption<int>("Large", 2)
}, (option, token) =>
{
    Size = option.Value;
    return Task.CompletedTask;
}), cancellationToken);
```

Validation returns an error string or null/empty for success. Errors render in
the text dialog without submitting. Both dialogs await their submission callback
before popping and reactivating the parent. Mutate parent state in the callback;
the parent's activation renders it. Callback failures stay visible to the host
and leave the dialog open. Back buttons and `/back` cancel without submitting.
Dialogs preserve incoming user messages and ignore non-text input when expecting
text. Push them as children; they are not designed as application root pages.

Picker options are snapshotted and typed. Duplicate display labels are valid.
Empty pickers still show the prompt and Back button. Keyboard callback IDs are
unique per dialog so a stale option cannot select from a newly opened picker.

## Forms

Construct form elements once in the owning view model, not during view rendering:

```csharp
NameField = new FormTextBox(this, "Name", validate: text =>
    string.IsNullOrWhiteSpace(text) ? "Required." : null);
SizeField = new FormPickerButton<int>(this, "Size", sizes);
SaveButton = new FormButton<string>(this, "Save", "profile", async (data, token) =>
{
    await repository.SaveAsync(NameField.Text, SizeField.SelectedValue, token);
    Status = "Saved.";
    await UpdateViewAsync(token);
});
Fields = new IFormElement[] { NameField, SizeField, SaveButton };
```

Each element registers its callback with the owning view model. FormTextBox and
FormPickerButton open child dialogs; their optional `onChanged` callback is
awaited before storing the new field value. A picker exposes `HasSelection`,
`SelectedValue` and `SelectedText`; check `HasSelection` for value types where a
default value could also be a valid choice. FormButton passes its current `Data`
to the async handler. Application handlers own persistence and validation rules.

The application's stateless view composes the elements:

```csharp
return FormRenderer.Render("Profile", model.Fields, backCallbackData: "/back");
```

Form elements expose state through `IFormElement`: Label, CallbackData and
DisplayValue (null for an action button). They contain no HTML formatting.
`FormRenderer` escapes the title, labels and field values and owns the HTML layout.
It renders a row of inline buttons per element and an optional Back button; the
owning view model must handle that back callback. `Display` is pure and never
creates or registers controls. Prompts/labels are plain text, not HTML templates.
Use validators to bound user input and keep composed messages within Telegram's
size limits. Pagination and application localization remain application concerns.

### Input button

`new FormInputButton(owner, "💬 Message", prompt, onSubmit, validate)` is a one-shot
action: it opens a text dialog and passes accepted text to the awaited
`onSubmit(text, token)` without storing it. Validation errors keep the dialog
open, and the owner renders on reactivation. Use it for "send"-style input and
`FormTextBox` for a field that keeps its value.

`FormTextBox.Clear()` resets a stored value and
`FormPickerButton<T>.Select(value)` preselects a matching option, returning false
if there is none.

### Toggle

`new FormToggle(owner, "Timestamps", value, onChanged, onText: "Shown", offText: "Hidden")`
is an `IFormElement` that flips a bool on click. It awaits `onChanged(next, token)`
before storing the value, then renders the owner. A failure leaves the value unchanged.

### Scroll box

`FormScrollBox<T>(owner, source, pageSize)` shows a page-sized window over a live
list (`Func<IReadOnlyList<T>>`, read at render time). In auto mode it follows the
newest items. Scrolling older switches to manual mode, which keeps the same
items visible while new ones arrive. Scrolling newer to the bottom, `ScrollToLatest`,
or the auto button resumes following. `GetWindow()` returns the visible items and
the hidden `Older`/`Newer` counts. Its five callbacks (oldest, older, newer,
latest, auto toggle) re-render the owner. Owners can also call the scroll
methods directly, e.g. jump to latest after the user posts.

### Layout

`FormLayout` composes a single message from sections and keyboard rows:

```csharp
return FormLayout.Create("Chat")
    .Html(trustedHtml)                                   // caller escapes dynamic parts
    .Scroll(model.History, item => HtmlText.Encode(item), "Empty", maxLength: 2800)
    .Text("plain text, escaped")
    .Fields(model.Fields)                                // same layout as FormRenderer
    .Buttons(("Settings", "/settings"), ("Leave", "/back"))
    .Back("/back")
    .Build();
```

`Scroll` renders `▲ N earlier` / `▼ N newer` markers and a navigation row. The
newest visible item is always shown. Older visible items that exceed `maxLength`
are folded into the earlier count, so long items stay within Telegram's
4096-character limit. `FormRenderer.Render(title, fields, back)` is shorthand for
`Create(title).Fields(fields).Back(back)`.

`HtmlText.Encode` escapes only `& < > "`, which is all Telegram HTML needs. Emoji
and other Unicode pass through unchanged, so they don't inflate message length.

The test bot's HatoChat (`TelegramTestBot/ViewModels/ChatRoomViewModel.cs`) uses
every control: text fields with validation, pickers, buttons, toggles and the
scroll box. The ToDo demo uses
the shared dialogs and no longer carries its own copies. For the full dependency
and folder conventions, see `../ARCHITECTURE.md`.

```bash
dotnet test UglyFwUI/Tests/UglyFwUI.Tests.csproj
```
